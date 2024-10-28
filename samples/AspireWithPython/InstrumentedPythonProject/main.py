import os
import logging
import flask
# from opentelemetry import trace
# from opentelemetry.exporter.otlp.proto.grpc.trace_exporter import OTLPSpanExporter
# from opentelemetry.sdk.trace import TracerProvider
# from opentelemetry.sdk.trace.export import BatchSpanProcessor
# from opentelemetry.instrumentation.flask import FlaskInstrumentor

app = flask.Flask(__name__)

# trace.set_tracer_provider(TracerProvider())
# otlpExporter = OTLPSpanExporter()
# processor = BatchSpanProcessor(otlpExporter)
# trace.get_tracer_provider().add_span_processor(processor)

# FlaskInstrumentor().instrument_app(app)

logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)

@app.route('/', methods=['GET'])
def hello_world():
    logger.info("request received!")
    return 'Hello, World!'

if __name__ == '__main__':
    port = int(os.environ.get('PORT', 8111))
    app.run(port=port, debug=True)
