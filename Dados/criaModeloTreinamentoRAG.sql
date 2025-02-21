CREATE EXTENSION IF NOT EXISTS vector;

CREATE TABLE treinamento_rag (
    id SERIAL PRIMARY KEY,
    pergunta TEXT NOT NULL,
    resposta TEXT NOT NULL,
    embedding VECTOR(768) -- Valor padrão que encontrei para a maioria dos modelos.
);
