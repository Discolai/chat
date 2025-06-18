ARG TARGETPLATFORM

FROM --platform=${TARGETPLATFORM} node:18 as build-frontend

WORKDIR /app

COPY ./src/Client/package.json ./
COPY ./src/Client/pnpm-lock.yaml ./

RUN npm i -g pnpm -y
RUN pnpm install

COPY ./src/Client .

RUN pnpm run build

FROM --platform=${TARGETPLATFORM} mcr.microsoft.com/dotnet/sdk:9.0 AS build-backend
WORKDIR /app

COPY ./src ./
COPY ./.editorconfig ./

COPY --from=build-frontend /app/dist Application/wwwroot

RUN dotnet restore
RUN dotnet publish -c Release -o out

FROM --platform=${TARGETPLATFORM} mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build-backend /app/out .

EXPOSE 8080

RUN mkdir data
VOLUME [ "/app/data" ]

ENTRYPOINT ["dotnet", "Application.dll"]