import http from 'k6/http';

var url = __ENV.SUT_URL || 'http://localhost:5000/Work';

export default function () {
  var payload = JSON.stringify({
    data: new Date()
  });

  var params = {
    headers: {
      'Content-Type': 'application/json',
    },
  };

  http.post(url, payload, params);
}
