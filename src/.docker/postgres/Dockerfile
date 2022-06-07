FROM postgres:14.2-alpine3.15

RUN mkdir -p /docker-entrypoint-initdb.d

COPY setup.sql /docker-entrypoint-initdb.d

EXPOSE 5432

