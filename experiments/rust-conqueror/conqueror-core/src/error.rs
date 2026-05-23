use thiserror::Error;

#[derive(Debug, Error)]
pub enum ConquerorError {
    #[error("no handler registered for message `{message}`")]
    HandlerNotFound { message: &'static str },

    #[error("registry entry for message `{message}` received an incompatible request type")]
    InvalidRequestType { message: &'static str },

    #[error("handler for message `{message}` returned an incompatible response type")]
    InvalidResponseType { message: &'static str },

    #[error("handler failed: {reason}")]
    HandlerFailed { reason: String },

    #[error("middleware failed: {reason}")]
    MiddlewareFailed { reason: String },
}

impl ConquerorError {
    pub fn handler_failed(reason: impl Into<String>) -> Self {
        Self::HandlerFailed {
            reason: reason.into(),
        }
    }

    pub fn middleware_failed(reason: impl Into<String>) -> Self {
        Self::MiddlewareFailed {
            reason: reason.into(),
        }
    }
}
