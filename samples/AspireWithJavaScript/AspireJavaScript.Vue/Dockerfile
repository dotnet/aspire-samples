FROM node:20.7

WORKDIR /app

COPY package.json package.json
COPY package-lock.json package-lock.json

RUN npm install

COPY . .

EXPOSE 8080
ENV PORT=8080
ENV NODE_ENV=production

CMD [ "npm", "run", "dev" ]