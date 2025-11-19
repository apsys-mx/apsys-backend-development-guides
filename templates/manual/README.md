# Templates del Manual de Construcción de Proyecto

Esta carpeta contiene los templates de código extraídos del [Manual de Construcción de Proyecto](../../docs/MANUAL_CONSTRUCCION_PROYECTO.md).

## Estructura

```
templates/manual/
├── paso-04-ndbunit/           # NDbUnit para gestión de datos de prueba
│   ├── INDbUnit.cs
│   ├── NDbUnit.cs
│   ├── PostgreSQLNDbUnit.cs
│   └── SqlServerNDbUnit.cs
├── paso-05-common-tests/      # Utilidades comunes para tests
│   └── AppSchemaExtender.cs
├── paso-07-infrastructure/    # Capa de infraestructura
│   ├── nhibernate/
│   │   ├── NHRepository.cs
│   │   ├── NHUnitOfWork.cs
│   │   └── NHSessionFactory.cs
│   └── tests/
│       ├── NHRepositoryTestInfrastructureBase.cs
│       └── NHRepositoryTestBase.cs
└── paso-09-scenarios/         # Generador de escenarios de prueba
    ├── IScenario.cs
    ├── CommandLineArgs.cs
    ├── ExitCode.cs
    ├── ScenarioBuilder.cs
    ├── Program.cs
    ├── Sc010CreateSandBox.cs
    └── Sc020CreateRoles.cs
```

## Uso

Estos templates deben usarse como referencia al seguir el manual. Para utilizarlos:

1. Copiar el archivo al proyecto correspondiente
2. Reemplazar `MiProyecto` por el nombre real del proyecto
3. Ajustar namespaces según la estructura del proyecto
4. Agregar entidades y validators específicos del dominio

## Notas Importantes

- Los templates usan el namespace `MiProyecto` como placeholder
- Los validators deben registrarse manualmente para cada entidad
- Los templates de NDbUnit incluyen versiones para PostgreSQL y SQL Server
- Solo usar la versión correspondiente a la base de datos del proyecto

## Proyecto de Referencia

Estos templates fueron extraídos y validados contra el proyecto de referencia:
`D:\apsys-mx\inspeccion-distancia\hashira.stone.backend`

## Actualización

Cuando se actualice el proyecto de referencia, estos templates deben ser revisados y actualizados para mantener la consistencia.
