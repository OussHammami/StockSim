# --- build stage ---
FROM node:20-alpine AS build
WORKDIR /app

# Install deps
COPY package*.json ./
RUN npm ci

# Copy source (context is src/StockSim.React)
COPY . .

# Build-time env for Vite (used to configure MarketFeed base URL in the frontend)
# Default to localhost:8081 for browser access; compose can override as needed.
ARG VITE_MARKETFEED_URL=http://localhost:8081
ENV VITE_MARKETFEED_URL=$VITE_MARKETFEED_URL

# Build production assets
RUN npm run build

# --- runtime stage ---
FROM nginx:alpine
# Copy built app
COPY --from=build /app/dist /usr/share/nginx/html

# SPA routing + prevent stale index.html
COPY nginx-default.conf /etc/nginx/conf.d/default.conf

EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]