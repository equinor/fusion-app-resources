FROM node:alpine as base

COPY .devcontainer.json ./

WORKDIR /app

COPY package*.json ./
RUN npm install --no-progress --no-optional

COPY . .

EXPOSE 3000

CMD npm run dev
