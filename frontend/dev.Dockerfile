FROM node:alpine

RUN apk add --no-cache \
  git \
  openssh

WORKDIR /app

COPY package*.json ./
RUN npm install

COPY . .

EXPOSE 3000

CMD npm run dev
