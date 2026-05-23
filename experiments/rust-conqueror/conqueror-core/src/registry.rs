use std::any::{Any, TypeId};
use std::collections::HashMap;
use std::sync::{Arc, Mutex};

use tower::util::{BoxCloneService, ServiceExt};
use tower::{service_fn, Service};

use crate::dispatch::Conqueror;
use crate::{ConquerorError, HandlerService, Message, MessageHandler, MessageRequest};

type ErasedRequest = Box<dyn Any + Send>;
type ErasedResponse = Box<dyn Any + Send>;
type ErasedMessageService = BoxCloneService<ErasedRequest, ErasedResponse, ConquerorError>;

#[derive(Clone)]
struct MessageRegistration {
    service: Arc<Mutex<ErasedMessageService>>,
}

#[derive(Clone, Default)]
pub struct MessageRegistry {
    registrations: HashMap<TypeId, MessageRegistration>,
}

impl MessageRegistry {
    fn insert<M>(&mut self, service: ErasedMessageService)
    where
        M: Message,
    {
        self.registrations
            .insert(TypeId::of::<M>(), MessageRegistration {
                service: Arc::new(Mutex::new(service)),
            });
    }

    pub(crate) fn service<M>(&self) -> Option<ErasedMessageService>
    where
        M: Message,
    {
        self.registrations
            .get(&TypeId::of::<M>())
            .map(|registration| registration.service.lock().unwrap().clone())
    }
}

#[derive(Default)]
pub struct ConquerorBuilder {
    registry: MessageRegistry,
}

impl ConquerorBuilder {
    pub fn new() -> Self {
        Self::default()
    }

    pub fn add_message<M, H>(mut self, handler: H) -> Self
    where
        M: Message,
        H: MessageHandler<M>,
    {
        let service = HandlerService::<H, M>::new(handler);
        self.registry.insert::<M>(erase_message_service::<M, _>(service));
        self
    }

    pub fn add_message_with<M, H, S, F>(mut self, handler: H, configure: F) -> Self
    where
        M: Message,
        H: MessageHandler<M>,
        S: Service<MessageRequest<M>, Response = M::Response, Error = ConquerorError>
            + Clone
            + Send
            + 'static,
        S::Future: Send + 'static,
        F: FnOnce(HandlerService<H, M>) -> S,
    {
        let service = configure(HandlerService::<H, M>::new(handler));
        self.registry.insert::<M>(erase_message_service::<M, _>(service));
        self
    }

    pub fn build(self) -> Conqueror {
        Conqueror::new(self.registry)
    }
}

fn erase_message_service<M, S>(service: S) -> ErasedMessageService
where
    M: Message,
    S: Service<MessageRequest<M>, Response = M::Response, Error = ConquerorError>
        + Clone
        + Send
        + 'static,
    S::Future: Send + 'static,
{
    let service = BoxCloneService::new(service);

    let erased = service_fn(move |request: ErasedRequest| {
        let service = service.clone();

        async move {
            let request = request
                .downcast::<MessageRequest<M>>()
                .map_err(|_| ConquerorError::InvalidRequestType { message: M::TAG })?;

            let response = service.oneshot(*request).await?;

            Ok(Box::new(response) as ErasedResponse)
        }
    });

    BoxCloneService::new(erased)
}
