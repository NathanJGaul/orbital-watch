# -- Build stage --
# Use the official pnpm Docker image as the base for the build stage
FROM ghcr.io/pnpm/pnpm:11 AS build
ENV CI=true
# Set Node.js version 22 globally using pnpm's runtime management
RUN pnpm runtime set node 22 -g
# Set the working directory to /app for all subsequent commands
WORKDIR /app

# Copy package.json, pnpm-lock.json and pnpm-workspace.yaml to install dependencies
COPY package.json pnpm-lock.yaml pnpm-workspace.yaml  ./
# pnpm ci requires pnpm-lock.json
RUN pnpm ci

# Copy the rest of the application source code into the container
COPY . .
# Run the build script to generate production assets
RUN pnpm run build

# -- Runtime stage: nginx serving the built static assets --
# Use the official nginx Alpine image as the runtime base
FROM nginx:1.27-alpine AS runtime
# Copy the build output from the build stage into nginx's web root directory
COPY --from=build /app/dist /usr/share/nginx/html
# Copy a custom nginx configuration file to the appropriate location
COPY nginx.conf /etc/nginx/conf.d/default.conf

# Expose port 80 for HTTP traffic
EXPOSE 80
