
import urllib.request
contents = urllib.request.urlopen("https://microsoftedge.github.io/Demos/json-dummy-data/64KB.json").read()
# contents = urllib.request.urlopen("https://apiservice").read()

print(contents)

# $HOME/moljac-python/venv/bin/pip3 install requests

import requests
r = requests.get("https://microsoftedge.github.io/Demos/json-dummy-data/64KB.json")
# r = requests.get("https://apiservice")

print(r.status_code)
print(r.headers)
print(r.content)  # bytes
print(r.text)     # r.content as str