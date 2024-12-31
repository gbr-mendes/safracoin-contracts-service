FROM node:20.12.2

WORKDIR /app

COPY package.json ./
RUN npm install

COPY ./contracts ./contracts
COPY ./hardhat.config.js ./hardhat.config.js

CMD ["npx", "hardhat", "compile"]
