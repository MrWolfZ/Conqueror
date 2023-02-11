# Conqueror recipe (CQS Advanced): exposing commands and queries via HTTP

_work-in-progress_

- mention how path and API group are set
- mention versioning
- mention status codes
- mention how this works by dynamically generating controllers
- include instructions for writing custom controllers in case built-in handling does not suffice
- If you are using the new simple `WebApplication` builder, call `FinalizeConquerorRegistrations` just before you call `var app = builder.Build();`. If you are using a `Startup.cs` file, call the method at the end of `ConfigureServices`.
