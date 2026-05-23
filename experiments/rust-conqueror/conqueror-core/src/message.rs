use crate::Context;

pub trait Message: Send + Sync + 'static {
    type Response: Send + Sync + 'static;

    const TAG: &'static str;
}

pub struct MessageRequest<M> {
    message: M,
    context: Context,
}

impl<M> MessageRequest<M> {
    pub fn new(message: M, context: Context) -> Self {
        Self { message, context }
    }

    pub fn message(&self) -> &M {
        &self.message
    }

    pub fn message_mut(&mut self) -> &mut M {
        &mut self.message
    }

    pub fn into_message(self) -> M {
        self.message
    }

    pub fn context(&self) -> &Context {
        &self.context
    }

    pub fn context_mut(&mut self) -> &mut Context {
        &mut self.context
    }

    pub fn into_parts(self) -> (M, Context) {
        (self.message, self.context)
    }
}
