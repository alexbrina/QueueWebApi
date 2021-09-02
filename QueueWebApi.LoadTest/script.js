import http from 'k6/http';

export default function () {
  var url = 'http://localhost:1376/Work';
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
