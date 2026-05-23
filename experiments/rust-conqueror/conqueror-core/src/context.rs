use std::collections::BTreeMap;
use std::fmt;

use uuid::Uuid;

#[derive(Clone, Debug, Eq, PartialEq, Hash)]
pub struct OperationId(Uuid);

impl OperationId {
    pub fn new() -> Self {
        Self(Uuid::new_v4())
    }

    pub fn as_uuid(&self) -> &Uuid {
        &self.0
    }
}

impl fmt::Display for OperationId {
    fn fmt(&self, f: &mut fmt::Formatter<'_>) -> fmt::Result {
        write!(f, "{}", self.0)
    }
}

#[derive(Clone, Debug, Default, Eq, PartialEq)]
pub struct ContextData {
    values: BTreeMap<String, String>,
}

impl ContextData {
    pub fn insert(&mut self, key: impl Into<String>, value: impl Into<String>) -> Option<String> {
        self.values.insert(key.into(), value.into())
    }

    pub fn get(&self, key: &str) -> Option<&str> {
        self.values.get(key).map(String::as_str)
    }

    pub fn is_empty(&self) -> bool {
        self.values.is_empty()
    }
}

#[derive(Clone, Debug, Eq, PartialEq)]
pub struct Context {
    operation_id: OperationId,
    data: ContextData,
}

impl Context {
    pub fn new() -> Self {
        Self {
            operation_id: OperationId::new(),
            data: ContextData::default(),
        }
    }

    pub fn operation_id(&self) -> &OperationId {
        &self.operation_id
    }

    pub fn data(&self) -> &ContextData {
        &self.data
    }

    pub fn data_mut(&mut self) -> &mut ContextData {
        &mut self.data
    }

    pub fn insert(&mut self, key: impl Into<String>, value: impl Into<String>) -> Option<String> {
        self.data.insert(key, value)
    }

    pub fn get(&self, key: &str) -> Option<&str> {
        self.data.get(key)
    }
}
