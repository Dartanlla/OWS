FROM postgres:14.2-alpine3.15

RUN mkdir -p /docker-entrypoint-initdb.d

COPY ./.docker/postgres/setup.sql /docker-entrypoint-initdb.d

EXPOSE 5432

