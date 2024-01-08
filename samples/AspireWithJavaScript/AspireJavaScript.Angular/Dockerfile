FROM node:20.7

WORKDIR /app

COPY package.json package.json
COPY package-lock.json package-lock.json

RUN npm i

COPY . .

EXPOSE 4200

CMD [ "npm", "start" ]