FROM nikolaik/python-nodejs:latest as base
WORKDIR /app

COPY package*.json ./
RUN npm ci

COPY . .

RUN npm run build