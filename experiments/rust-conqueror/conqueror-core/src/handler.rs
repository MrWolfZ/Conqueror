use std::future::Future;
use std::marker::PhantomData;
use std::pin::Pin;
use std::sync::Arc;
use std::task::{Context as TaskContext, Poll};

use tower::Service;

use crate::{ConquerorError, Message, MessageRequest};

#[async_trait::async_trait]
pub trait MessageHandler<M>: Send + Sync + 'static
where
    M: Message,
{
    async fn handle(&self, request: MessageRequest<M>) -> Result<M::Response, ConquerorError>;
}

pub struct HandlerService<H, M> {
    handler: Arc<H>,
    marker: PhantomData<fn(M)>,
}

impl<H, M> HandlerService<H, M> {
    pub fn new(handler: H) -> Self {
        Self {
            handler: Arc::new(handler),
            marker: PhantomData,
        }
    }
}

impl<H, M> Clone for HandlerService<H, M> {
    fn clone(&self) -> Self {
        Self {
            handler: Arc::clone(&self.handler),
            marker: PhantomData,
        }
    }
}

impl<H, M> Service<MessageRequest<M>> for HandlerService<H, M>
where
    H: MessageHandler<M>,
    M: Message,
{
    type Response = M::Response;
    type Error = ConquerorError;
    type Future = Pin<Box<dyn Future<Output = Result<Self::Response, Self::Error>> + Send>>;

    fn poll_ready(&mut self, _cx: &mut TaskContext<'_>) -> Poll<Result<(), Self::Error>> {
        Poll::Ready(Ok(()))
    }

    fn call(&mut self, request: MessageRequest<M>) -> Self::Future {
        let handler = Arc::clone(&self.handler);

        Box::pin(async move { handler.handle(request).await })
    }
}
