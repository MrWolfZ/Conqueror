curl http://localhost:5000/api/signals/sse?signalEventType=counterIncremented

# follow the above by this in another shell to see the signal:
# curl http://localhost:5000/api/v1/incrementCounterByAmount \
# --data '{"counterName":"sseTest","incrementBy":2}' \
# -H 'Content-Type: application/json'

# this prints something like this on the SSE stream:
# event: counterIncremented
# data: {"counterName":"sseTest","newValue":2,"incrementBy":2}
# data: d|conqueror-message-id:5227cf9ead99f26b|trace-id:b107e7cb47d8951996339d99baf4cd28
# id: 2d98b4aca7ee02df
