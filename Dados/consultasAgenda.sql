select pes.nome as pessoa, pai.nome as pai, mae.nome as mae
from pessoa pes, 
     pessoa pai, 
     pessoa mae, 
     associacaopessoa assocpai,
     associacaopessoa assocmae
where 
  (
    (assocmae.pessoaid = pes.objectid AND assocmae.pessoaassociadaid = mae.objectid and assocmae.tipo = 2)
   AND
    (assocpai.pessoaid = pes.objectid AND assocpai.pessoaassociadaid = pai.objectid and assocpai.tipo = 1)
  ) 
AND  pes.objectid = '3';

