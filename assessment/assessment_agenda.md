# Assessment Decision Register

| # | Decision | Date | Options | Choice | Rationale |
|---|----------|------|---------|--------|-----------|
| 1 | System identification | 2026-08-31 | Shopizer 3.2.7 or other label | Shopizer 3.2.7 | Inferred from the loaded source path and project metadata. |
| 2 | Business domain | 2026-08-31 | E-commerce or other domain | E-commerce | Evident from catalog, order, customer, merchant, and storefront modules. |
| 3 | Legacy stack | 2026-08-31 | Java/Maven/Spring/Drools or other stack | Java/Maven/Spring/Drools | Confirmed from source structure and build/configuration files. |
| 4 | Analysis mode | 2026-08-31 | Direct Source, CAST Imaging, Hybrid | Hybrid | CAST covers structural evidence while source reading is retained for detailed business rules. |
| 5 | Target stack (assumed) | 2026-08-31 | .NET 10, TypeScript/NestJS, Java/Spring Boot, Python/FastAPI, TBD | .NET 10 | Operator preference; subject to evidence-based confirmation in Phase 4b. |
| 6 | CAST integration status | 2026-08-31 | Verified, pending remediation | Verified for `Shopizer-Backend` | CAST application inventory, statistics, and dependency queries now succeed. |
| 7 | Naming conventions | 2026-08-31 | Source/docs evidence or operator guidance | Source/docs evidence | `sm-*` modules, `com.salesmanager...` packages, interface/implementation suffixes, versioned API models, REST/Swagger, `SALESMANAGER` schema. |
| 8 | Engagement scope | 2026-08-31 | Backend only or backend plus loaded frontends | Backend plus `Shopizer-WebAdmin` and `Shopizer-WebFrontEnd` | Operator added both frontend source trees under `initial-source`; scope expanded to three analyzed applications and eight segments. |
| 9 | Phase 0 exit approval | 2026-08-31 | Approve or hold | Approved | Operator approved Phase 0 and authorized Phase 1 and Phase 2 to proceed in parallel. |
| 8 | Engagement scope | 2026-08-31 | Backend only or backend plus loaded frontends | Backend plus `Shopizer-WebAdmin` and `Shopizer-WebFrontEnd` | Operator added both frontend source trees under `initial-source`; scope expanded to three analyzed applications and eight segments. |
