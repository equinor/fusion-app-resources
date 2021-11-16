FROM nikolaik/python-nodejs:14 as base
WORKDIR /app

COPY package*.json ./
RUN npm ci

COPY . .

RUN npm run build