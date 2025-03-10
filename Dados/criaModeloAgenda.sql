DO $$
BEGIN
   --DROP TABLE IF EXISTS itemdominio; 
   --DROP TABLE IF EXISTS dominio;
   
   --DROP TABLE IF EXISTS associacaopessoa;
   --DROP TABLE IF EXISTS pessoa;
   
   --DROP TABLE IF EXISTS pessoatelefone;
   DROP TABLE IF EXISTS pessoaendereco;
    
    IF NOT EXISTS (SELECT * FROM information_schema.tables 
                   WHERE table_name = 'dominio') THEN
        CREATE TABLE dominio (
            objectid VARCHAR (36) PRIMARY KEY,
    		classid INTEGER,
            codigo INTEGER NOT NULL UNIQUE, 
   		    descricao VARCHAR(100) NOT NULL
         );
    END IF;
 
   IF NOT EXISTS (SELECT * FROM information_schema.tables 
                   WHERE table_name = 'itemdominio') THEN
        CREATE TABLE itemdominio (
            objectid VARCHAR (36) PRIMARY KEY,
    		classid INTEGER,
            codigo INTEGER NOT NULL, 
            sigla VARCHAR (15),
   		    descricao VARCHAR(100) NOT NULL,
            dominioid VARCHAR (36),
            FOREIGN KEY (dominioid) REFERENCES dominio(objectid)
        );
    END IF;

 
    IF NOT EXISTS (SELECT * FROM information_schema.tables 
                   WHERE table_name = 'pessoa') THEN
        CREATE TABLE pessoa (
            objectid VARCHAR (36) PRIMARY KEY,
    		classid INTEGER, 
   		    nome VARCHAR(100) NOT NULL,
            sexo INTEGER,
            dtnascimentooucriacao DATE NOT NULL,
            email VARCHAR(255) UNIQUE,
            telefone VARCHAR(15),
            cpfcnpj varchar (14)
        );
    END IF;

    IF NOT EXISTS (SELECT * FROM information_schema.tables 
                   WHERE table_name = 'associacaopessoa') THEN
        CREATE TABLE associacaopessoa (
            objectid VARCHAR (36) PRIMARY KEY,
    		classid INTEGER,
            tipo INTEGER,
            pessoaid VARCHAR (36),
            pessoaassociadaid VARCHAR (36), 
            FOREIGN KEY (pessoaid) REFERENCES pessoa(objectid),
 		    FOREIGN KEY (pessoaassociadaid) REFERENCES pessoa(objectid)
        );
    END IF;

    IF NOT EXISTS (SELECT * FROM information_schema.tables 
                   WHERE table_name = 'pessoatelefone') THEN
        CREATE TABLE pessoatelefone (
            objectid VARCHAR (36) PRIMARY KEY,
    		classid INTEGER,
            tipo INTEGER,
            pessoaid VARCHAR (36),
            telefone VARCHAR (15), 
            FOREIGN KEY (pessoaid) REFERENCES pessoa(objectid)			
        );
    END IF;

	IF NOT EXISTS (SELECT * FROM information_schema.tables 
                   WHERE table_name = 'endereco') THEN
        CREATE TABLE endereco (
            objectid VARCHAR (36) PRIMARY KEY,
    		classid INTEGER,
            endereco VARCHAR(100), 
			numero VARCHAR(10),
			cep VARCHAR(36)			
        );
    END IF;

	IF NOT EXISTS (SELECT * FROM information_schema.tables 
                   WHERE table_name = 'associacaopessoaendereco') THEN
        CREATE TABLE associacaopessoaendereco (
            objectid VARCHAR (36) PRIMARY KEY,
    		classid INTEGER,
            tipo INTEGER,
            pessoaid VARCHAR (36),
            enderecoid VARCHAR (36), 
            FOREIGN KEY (pessoaid) REFERENCES pessoa(objectid),
			FOREIGN KEY (enderecoid) REFERENCES endereco(objectid)			
        );
    END IF;
	
END $$;