use std::any::Any;
use std::sync::Arc;

use tower::util::ServiceExt;

use crate::{ConquerorBuilder, ConquerorError, Context, Message, MessageRegistry, MessageRequest};

#[derive(Clone)]
pub struct Conqueror {
    registry: Arc<MessageRegistry>,
}

impl Conqueror {
    pub fn builder() -> ConquerorBuilder {
        ConquerorBuilder::new()
    }

    pub(crate) fn new(registry: MessageRegistry) -> Self {
        Self {
            registry: Arc::new(registry),
        }
    }

    pub fn messages(&self) -> MessageSenders {
        MessageSenders {
            dispatcher: MessageDispatcher {
                registry: Arc::clone(&self.registry),
            },
        }
    }
}

#[derive(Clone)]
pub struct MessageDispatcher {
    registry: Arc<MessageRegistry>,
}

impl MessageDispatcher {
    pub async fn dispatch<M>(&self, message: M, context: Option<Context>) -> Result<M::Response, ConquerorError>
    where
        M: Message,
    {
        let service = self
            .registry
            .service::<M>()
            .ok_or(ConquerorError::HandlerNotFound { message: M::TAG })?;

        let context = context.unwrap_or_else(Context::new);
        let request = MessageRequest::new(message, context);
        let response = service
            .oneshot(Box::new(request) as Box<dyn Any + Send>)
            .await?;

        response
            .downcast::<M::Response>()
            .map(|response| *response)
            .map_err(|_| ConquerorError::InvalidResponseType { message: M::TAG })
    }
}

#[derive(Clone)]
pub struct MessageSenders {
    dispatcher: MessageDispatcher,
}

impl MessageSenders {
    pub async fn send<M>(&self, message: M) -> Result<M::Response, ConquerorError>
    where
        M: Message,
    {
        self.dispatcher.dispatch(message, None).await
    }

    pub async fn send_with_context<M>(&self, message: M, context: Context) -> Result<M::Response, ConquerorError>
    where
        M: Message,
    {
        self.dispatcher.dispatch(message, Some(context)).await
    }
}
