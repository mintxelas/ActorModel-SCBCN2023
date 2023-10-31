# ActorModel

Este repositorio contiene el código en C# utilizado en la presentación de la SCBCN 2023 "Introducción al modelo de Actores".

## Caso de uso

Se implementa la transferencia de dinero entre dos cuentas. Cada cuenta está en una única moneda. El importe a transferir ha de especificarse en la moneda de la cuenta de origen. El importe que recibe la cuenta de destino puede estar en una moneda diferente, por ello se incluye un servicio de cambio (exchange).

Si no hay saldo en la cuenta de origen la transferencia falla. Si no hay factor de cambio entre las monedas origen y destino en el Exchange, la transferencia falla.

## Contenido

Hay 2 proyectos ejecutables (aplicaciones de consola) que comparan el escenario de transferencia bancaria implementado en uno con Multithreading y el otro con Actores (usando el framework Akka.net).
Se crean 1000 cuentas en la base de datos y se dan de alta los factores de cambio entre EUR y GBP para las pruebas.

El proyecto ***Locking.Application*** contiene el ejemplo con multithreading y locks, el proyecto ***ActorModel.Application*** contiene el ejemplo con actores.
Pueden ejecutarse por separado o a la vez. Si el objetivo es comparar los rendimientos de cada uno, lo más fácil es lanzar ambos a la vez y observar las diferencias en grafana.

En los paneles de Grafana pueden verse las comparativas (ver la sección ***docker*** más abajo).

### docker:
Lanzar desde la carpeta ***docker/*** con ***docker compose up -d*** para arrancar las dependencias:
* prometheus
* grafana
* cadvisor
* postgres
* pgadmin
* exporter de postgres

Grafana viene con un dashboard que muestra gráficos de rendimiento de las apps de prueba.
Es accesible vía ***http://localhost:3000/*** y el usuario es ***admin/admin***.
(Los passwords por defecto pueden verse en el archivo ***docker/compose.yaml***).

