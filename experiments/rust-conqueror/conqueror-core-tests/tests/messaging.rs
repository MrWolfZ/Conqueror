use std::future::Future;
use std::pin::Pin;
use std::sync::{Arc, Mutex};
use std::task::{Context as TaskContext, Poll};

use conqueror_core::{Conqueror, ConquerorError, Context, Message, MessageHandler, MessageRequest};
use tower::{service_fn, Layer, Service, ServiceBuilder};

struct GetCounterValue {
    counter_name: String,
}

#[derive(Debug, Eq, PartialEq)]
struct GetCounterValueResponse {
    value: i32,
}

impl Message for GetCounterValue {
    type Response = GetCounterValueResponse;

    const TAG: &'static str = "get-counter-value";
}

struct GetCounterValueHandler;

#[async_trait::async_trait]
impl MessageHandler<GetCounterValue> for GetCounterValueHandler {
    async fn handle(
        &self,
        request: MessageRequest<GetCounterValue>,
    ) -> Result<GetCounterValueResponse, ConquerorError> {
        let value = match request.message().counter_name.as_str() {
            "orders" => 42,
            _ => 0,
        };

        Ok(GetCounterValueResponse { value })
    }
}

struct UnregisteredMessage;

impl Message for UnregisteredMessage {
    type Response = ();

    const TAG: &'static str = "unregistered-message";
}

struct FailingMessage;

impl Message for FailingMessage {
    type Response = ();

    const TAG: &'static str = "failing-message";
}

struct FailingHandler;

#[async_trait::async_trait]
impl MessageHandler<FailingMessage> for FailingHandler {
    async fn handle(&self, _request: MessageRequest<FailingMessage>) -> Result<(), ConquerorError> {
        Err(ConquerorError::handler_failed("boom"))
    }
}

struct ContextMessage;

#[derive(Debug, Eq, PartialEq)]
struct ContextResponse {
    operation_id: String,
    tenant: Option<String>,
}

impl Message for ContextMessage {
    type Response = ContextResponse;

    const TAG: &'static str = "context-message";
}

struct ContextHandler;

#[async_trait::async_trait]
impl MessageHandler<ContextMessage> for ContextHandler {
    async fn handle(&self, request: MessageRequest<ContextMessage>) -> Result<ContextResponse, ConquerorError> {
        Ok(ContextResponse {
            operation_id: request.context().operation_id().to_string(),
            tenant: request.context().get("tenant").map(str::to_owned),
        })
    }
}

struct RecordingMessage;

impl Message for RecordingMessage {
    type Response = Vec<&'static str>;

    const TAG: &'static str = "recording-message";
}

struct RecordingHandler {
    events: Arc<Mutex<Vec<&'static str>>>,
}

#[async_trait::async_trait]
impl MessageHandler<RecordingMessage> for RecordingHandler {
    async fn handle(&self, _request: MessageRequest<RecordingMessage>) -> Result<Vec<&'static str>, ConquerorError> {
        self.events.lock().unwrap().push("handler");

        Ok(self.events.lock().unwrap().clone())
    }
}

#[derive(Clone)]
struct RecordingLayer {
    events: Arc<Mutex<Vec<&'static str>>>,
}

impl RecordingLayer {
    fn new(events: Arc<Mutex<Vec<&'static str>>>) -> Self {
        Self { events }
    }
}

impl<S> Layer<S> for RecordingLayer {
    type Service = RecordingService<S>;

    fn layer(&self, inner: S) -> Self::Service {
        RecordingService {
            inner,
            events: Arc::clone(&self.events),
        }
    }
}

#[derive(Clone)]
struct RecordingService<S> {
    inner: S,
    events: Arc<Mutex<Vec<&'static str>>>,
}

impl<S, Req> Service<Req> for RecordingService<S>
where
    S: Service<Req, Error = ConquerorError> + Clone + Send + 'static,
    S::Future: Send + 'static,
    S::Response: Send + 'static,
    Req: Send + 'static,
{
    type Response = S::Response;
    type Error = ConquerorError;
    type Future = Pin<Box<dyn Future<Output = Result<Self::Response, Self::Error>> + Send>>;

    fn poll_ready(&mut self, cx: &mut TaskContext<'_>) -> Poll<Result<(), Self::Error>> {
        self.inner.poll_ready(cx)
    }

    fn call(&mut self, request: Req) -> Self::Future {
        let clone = self.inner.clone();
        let mut inner = std::mem::replace(&mut self.inner, clone);
        let events = Arc::clone(&self.events);

        Box::pin(async move {
            events.lock().unwrap().push("before");
            let response = inner.call(request).await;
            events.lock().unwrap().push("after");
            response
        })
    }
}

#[tokio::test]
async fn dispatches_registered_message_to_handler() {
    let app = Conqueror::builder()
        .add_message::<GetCounterValue, _>(GetCounterValueHandler)
        .build();

    let response = app
        .messages()
        .send(GetCounterValue {
            counter_name: "orders".to_owned(),
        })
        .await
        .unwrap();

    assert_eq!(response, GetCounterValueResponse { value: 42 });
}

#[tokio::test]
async fn returns_error_when_handler_is_not_registered() {
    let app = Conqueror::builder().build();

    let error = app.messages().send(UnregisteredMessage).await.unwrap_err();

    match error {
        ConquerorError::HandlerNotFound { message } => {
            assert_eq!(message, UnregisteredMessage::TAG);
        }
        error => panic!("unexpected error: {error:?}"),
    }
}

#[tokio::test]
async fn propagates_handler_errors() {
    let app = Conqueror::builder()
        .add_message::<FailingMessage, _>(FailingHandler)
        .build();

    let error = app.messages().send(FailingMessage).await.unwrap_err();

    assert!(matches!(
        error,
        ConquerorError::HandlerFailed { reason } if reason == "boom"
    ));
}

#[tokio::test]
async fn middleware_observes_before_and_after_handler() {
    let events = Arc::new(Mutex::new(Vec::new()));

    let app = Conqueror::builder()
        .add_message_with::<RecordingMessage, _, _, _>(
            RecordingHandler {
                events: Arc::clone(&events),
            },
            {
                let events = Arc::clone(&events);

                move |service| {
                    ServiceBuilder::new()
                        .layer(RecordingLayer::new(events))
                        .service(service)
                }
            },
        )
        .build();

    let response = app.messages().send(RecordingMessage).await.unwrap();

    assert_eq!(response, vec!["before", "handler"]);
    assert_eq!(
        events.lock().unwrap().as_slice(),
        ["before", "handler", "after"]
    );
}

#[tokio::test]
async fn middleware_can_short_circuit_handler() {
    let app = Conqueror::builder()
        .add_message_with::<GetCounterValue, _, _, _>(GetCounterValueHandler, |_service| {
            service_fn(|_request: MessageRequest<GetCounterValue>| async {
                Ok(GetCounterValueResponse { value: 999 })
            })
        })
        .build();

    let response = app
        .messages()
        .send(GetCounterValue {
            counter_name: "orders".to_owned(),
        })
        .await
        .unwrap();

    assert_eq!(response, GetCounterValueResponse { value: 999 });
}

#[tokio::test]
async fn supplied_context_reaches_handler() {
    let app = Conqueror::builder()
        .add_message::<ContextMessage, _>(ContextHandler)
        .build();

    let mut context = Context::new();
    context.insert("tenant", "acme");
    let operation_id = context.operation_id().to_string();

    let response = app
        .messages()
        .send_with_context(ContextMessage, context)
        .await
        .unwrap();

    assert_eq!(
        response,
        ContextResponse {
            operation_id,
            tenant: Some("acme".to_owned()),
        }
    );
}

#[tokio::test]
async fn root_context_gets_unique_operation_id_per_dispatch() {
    let app = Conqueror::builder()
        .add_message::<ContextMessage, _>(ContextHandler)
        .build();

    let first = app.messages().send(ContextMessage).await.unwrap();
    let second = app.messages().send(ContextMessage).await.unwrap();

    assert_ne!(first.operation_id, second.operation_id);
}
