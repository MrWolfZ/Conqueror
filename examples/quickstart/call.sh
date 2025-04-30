curl http://localhost:5000/api/v1/incrementCounterByAmount \
  --data '{"counterName":"test","incrementBy":2}' \
  -H 'Content-Type: application/json'
# prints {"newCounterValue":2}

curl http://localhost:5000/api/v1/getCounters?prefix=tes
# prints [{"counterName":"test","value":2}]

# this doubles the increment operation through a signal handler
curl http://localhost:5000/api/v1/incrementCounterByAmount \
  --data '{"counterName":"doubler","incrementBy":2}' \
  -H 'Content-Type: application/json'
# prints {"newCounterValue":4}

curl http://localhost:5000/api/v1/getCounters
# prints [{"counterName":"test","value":2},{"counterName":"doubler","value":4}]

# add a confidential counter
curl http://localhost:5000/api/v1/incrementCounterByAmount \
  --data '{"counterName":"confidential","incrementBy":1000}' \
  -H 'Content-Type: application/json'
# prints {"newCounterValue":1000}

curl http://localhost:5000/api/v1/getCounters
# prints [{"counterName":"test","value":2},{"counterName":"doubler","value":4},{"counterName":"confidential","value":1000}]
