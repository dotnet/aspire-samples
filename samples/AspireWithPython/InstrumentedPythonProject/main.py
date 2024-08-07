import os
import flask
import logging

logging.basicConfig()
logging.getLogger().setLevel(logging.NOTSET)

app = flask.Flask(__name__)

@app.route('/', methods=['GET'])
def hello_world():
    logging.getLogger(__name__).info("request received!")
    return 'Hello, World!'

if __name__ == '__main__':
    port = int(os.environ.get('PORT', 8111))
    app.run(host='0.0.0.0', port=port)