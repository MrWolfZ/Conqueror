# Design considerations: Dependency Injection API

```cs
// option 1

services.AddConquerorCQS() // returns IServiceCollection

        // register handlers
        .AddTransient<MyTransientCommandHandler>()
        .AddScoped(p => new MyFactoryCommandHandler())
        .AddSingleton<MySingletonQueryHandler>()
        .AddSingleton(new MySingletonQueryHandler())

        // register custom middlewares
        .AddTransient<MyTransientCommandMiddleware>()
        .AddScoped(p => new MyFactoryCommandMiddleware())
        .AddSingleton<MySingletonQueryMiddleware>()
        .AddSingleton(new MySingletonQueryMiddleware())

        // register pre-built middlewares
        .AddConquerorCQSLoggingMiddlewares()

        // register clients
        .AddConquerorCommandClient<IMyExternalCommandHandler>(b => b.UseHttp(serverBaseAddress))
        .AddConquerorQueryClient<IMyExternalQueryHandler>(b => b.UseHttp(serverBaseAddress))

        // register remaining types
        .AddConquerorCQSTypesFromExecutingAssembly();

services.AddConquerorEventing()
        .AddTransient<MyEventObserver>()
        .AddTransient<MyEventObserverMiddleware>()
        .AddConquerorEventingLoggingMiddlewares()
        .AddConquerorEventingClient<IMyExternalEventObserver>(b => b.UseHttp(serverBaseAddress))
        .AddConquerorEventingTypesFromExecutingAssembly();

services.AddControllers()
        .AddConquerorCQSHttpControllers()
        .AddConquerorEventingHttpControllers();

// things finalize does:
// - register handlers via their interfaces
// - add handler metadata into registry
// - sanity checks (e.g. multiple handlers for same command, handler or middleware with multiple interfaces etc.)
// - create invoker for each middleware
// - add middleware metadata into registry
// - create HTTP controllers
services.FinalizeConquerorRegistrations();

// pros:
// - just normal registrations
// - only need to call AddConquerorCQS once
// - no overloads for registration methods to maintain
// cons:
// - needing to call FinalizeConquerorRegistrations is error-prone and not intuitive
// - risk of wrongly using the API (e.g. registering handlers for an interface)
// - inconsistent registration of clients and handlers
// - certain methods need "Conqueror", "ConquerorCQS", etc. as part of their name

// option 2a

services.AddConquerorCQS() // returns IConquerorCQSBuilder

        // register handlers
        .AddTransient<MyTransientCommandHandler>()
        .AddScoped(p => new MyFactoryCommandHandler())
        .AddSingleton<MySingletonQueryHandler>()
        .AddSingleton(new MySingletonQueryHandler())
        
        // register custom middlewares
        .AddTransient<MyTransientCommandMiddleware>()
        .AddScoped(p => new MyFactoryCommandMiddleware())
        .AddSingleton<MySingletonQueryMiddleware>()
        .AddSingleton(new MySingletonQueryMiddleware())

        // register pre-built middlewares
        .AddLoggingMiddlewares() // needs no prefix

        // register clients
        .AddCommandClient<IMyExternalCommandHandler>(b => b.UseHttp(serverBaseAddress))
        .AddQueryClient<IMyExternalQueryHandler>(b => b.UseHttp(serverBaseAddress))

        // register remaining types
        .AddTypesFromExecutingAssembly();

services.AddConquerorEventing() // returns IConquerorEventingBuilder
        .AddTransient<MyEventObserver>()
        .AddTransient<MyEventObserverMiddleware>()
        .AddLoggingMiddlewares()
        .AddClient<IMyExternalEventObserver>(b => b.UseHttp(serverBaseAddress))
        .AddTypesFromExecutingAssembly();

// pros:
// - very similar to normal registrations
// - consistent registration of clients and handlers
// - no need for "Conqueror", "ConquerorCQS" etc. as part of method names except "AddConqueror..."
// cons:
// - requires calling AddConquerorCQS every time
// - need to create and maintain lots of overloads
// - cannot use generic constraints
// - very similar to normal registrations (i.e. risk of wrongly using the API)

// option 2b

services.AddConquerorCQS() // returns IConquerorCQSBuilder

        // register handlers
        .AddTransientCommandHandler<MyTransientCommandHandler>()
        .AddScopedCommandHandler(p => new MyFactoryCommandHandler())
        .AddSingletonQueryHandler<MySingletonQueryHandler>()
        .AddSingletonQueryHandler(new MySingletonQueryHandler())
        
        // register custom middlewares
        .AddTransientCommandMiddleware<MyTransientCommandMiddleware>()
        .AddScopedCommandMiddleware(p => new MyFactoryCommandMiddleware())
        .AddSingletonQueryMiddleware<MySingletonQueryMiddleware>()
        .AddSingletonQueryMiddleware(new MySingletonQueryMiddleware())

        // register pre-built middlewares
        .AddLoggingMiddlewares()

        // register clients
        .AddCommandClient<IMyExternalCommandHandler>(b => b.UseHttp(serverBaseAddress))
        .AddQueryClient<IMyExternalQueryHandler>(b => b.UseHttp(serverBaseAddress))

        // register remaining types
        .AddTypesFromExecutingAssembly();

services.AddConquerorEventing() // returns IConquerorEventingBuilder
        .AddTransientEventObserver<MyEventObserver>()
        .AddTransientEventObserverMiddleware<MyEventObserverMiddleware>()
        .AddLoggingMiddlewares()
        .AddClient<IMyExternalEventObserver>(b => b.UseHttp(serverBaseAddress))
        .AddTypesFromExecutingAssembly();

// pros:
// - very explicit
// - somewhat similar to normal registrations
// - safe due to generic constraints
// - consistent registration of clients and handlers
// - no need for "Conqueror", "ConquerorCQS" etc. as part of method names except "AddConqueror..."
// cons:
// - requires calling AddConquerorCQS every time
// - need to create and maintain lots of overloads

// option 2c

services.AddConquerorCQS() // returns IConquerorCQSBuilder

        // register handlers
        .AddCommandHandler<MyTransientCommandHandler>()
        .AddCommandHandler(p => new MyFactoryCommandHandler(), ServiceLifetime.Scoped)
        .AddQueryHandler<MySingletonQueryHandler>(ServiceLifetime.Singleton)
        .AddQueryHandler(new MySingletonQueryHandler())
        
        // register custom middlewares
        .AddCommandMiddleware<MyTransientCommandMiddleware>()
        .AddCommandMiddleware(p => new MyFactoryCommandMiddleware(), ServiceLifetime.Scoped)
        .AddQueryMiddleware<MySingletonQueryMiddleware>(ServiceLifetime.Singleton)
        .AddQueryMiddleware(new MySingletonQueryMiddleware())

        // register pre-built middlewares
        .AddLoggingMiddlewares()

        // register clients
        .AddCommandClient<IMyExternalCommandHandler>(b => b.UseHttp(serverBaseAddress))
        .AddQueryClient<IMyExternalQueryHandler>(b => b.UseHttp(serverBaseAddress))

        // register remaining types
        .AddTypesFromExecutingAssembly();

services.AddConquerorEventing() // returns IConquerorEventingBuilder
        .AddEventObserver<MyEventObserver>(ServiceLifetime.Singleton)
        .AddEventObserverMiddleware<MyEventObserverMiddleware>(ServiceLifetime.Singleton)
        .AddLoggingMiddlewares()
        .AddClient<IMyExternalEventObserver>(b => b.UseHttp(serverBaseAddress))
        .AddTypesFromExecutingAssembly();

// pros:
// - very explicit
// - safe due to generic constraints
// - need to create and maintain less overloads than 2b
// - consistent registration of clients and handlers
// - no need for "Conqueror", "ConquerorCQS" etc. as part of method names except "AddConqueror..."
// cons:
// - requires calling AddConquerorCQS every time
// - different API than normal registrations

// option 3a

services.AddConquerorCQS((IConquerorCQSBuilder b) =>
{
    // register handlers
    b.AddTransient<MyTransientCommandHandler>()
     .AddScoped(p => new MyFactoryCommandHandler())
     .AddSingleton<MySingletonQueryHandler>()
     .AddSingleton(new MySingletonQueryHandler());

    // register custom middlewares
    b.AddTransient<MyTransientCommandMiddleware>()
     .AddScoped(p => new MyFactoryCommandMiddleware())
     .AddSingleton<MySingletonQueryMiddleware>()
     .AddSingleton(new MySingletonQueryMiddleware());
    
    // register pre-built middlewares
    b.AddLoggingMiddlewares();

    // register clients
    b.AddCommandClient<IMyExternalCommandHandler>(b => b.UseHttp(serverBaseAddress))
     .AddQueryClient<IMyExternalQueryHandler>(b => b.UseHttp(serverBaseAddress));

    // register remaining types
    b.AddTypesFromExecutingAssembly();
});

services.AddConquerorEventing((IConquerorEventingBuilder b) =>
{
    b.AddTransient<MyEventObserver>()
     .AddTransient<MyEventObserverMiddleware>()
     .AddLoggingMiddlewares()
     .AddClient<IMyExternalEventObserver>(b => b.UseHttp(serverBaseAddress))
     .AddTypesFromExecutingAssembly();
});

// pros:
// - very similar to normal registrations
// - more clear than 1a that these are conqueror registrations due to the nesting
// - consistent registration of clients and handlers
// - no need for "Conqueror", "ConquerorCQS" etc. as part of method names except "AddConqueror..."
// cons:
// - requires calling AddConquerorCQS every time
// - need to create and maintain lots of overloads
// - cannot use generic constraints
// - very similar to normal registrations (i.e. risk of wrongly using the API)

// option 3b

services.AddConquerorCQS((IConquerorCQSBuilder b) =>
{
    // register handlers
    b.AddTransientCommandHandler<MyTransientCommandHandler>()
     .AddScopedCommandHandler(p => new MyFactoryCommandHandler())
     .AddSingletonQueryHandler<MySingletonQueryHandler>()
     .AddSingletonQueryHandler(new MySingletonQueryHandler());

    // register custom middlewares
    b.AddTransientCommandMiddleware<MyTransientCommandMiddleware>()
     .AddScopedCommandMiddleware(p => new MyFactoryCommandMiddleware())
     .AddSingletonQueryMiddleware<MySingletonQueryMiddleware>()
     .AddSingletonQueryMiddleware(new MySingletonQueryMiddleware());
    
    // register pre-built middlewares
    b.AddLoggingMiddlewares();

    // register clients
    b.AddCommandClient<IMyExternalCommandHandler>(b => b.UseHttp(serverBaseAddress))
     .AddQueryClient<IMyExternalQueryHandler>(b => b.UseHttp(serverBaseAddress));

    // register remaining types
    b.AddTypesFromExecutingAssembly();
});

services.AddConquerorEventing((IConquerorEventingBuilder b) =>
{
    b.AddTransientEventObserver<MyEventObserver>()
     .AddTransientEventObserverMiddleware<MyEventObserverMiddleware>()
     .AddLoggingMiddlewares()
     .AddClient<IMyExternalEventObserver>(b => b.UseHttp(serverBaseAddress))
     .AddTypesFromExecutingAssembly();
});

// pros:
// - very explicit
// - somewhat similar to normal registrations
// - safe due to generic constraints
// - consistent registration of clients and handlers
// - no need for "Conqueror", "ConquerorCQS" etc. as part of method names except "AddConqueror..."
// cons:
// - requires calling AddConquerorCQS every time
// - need to create and maintain lots of overloads

// option 3c

services.AddConquerorCQS((IConquerorCQSBuilder b) =>
{
    // register handlers
    b.AddCommandHandler<MyTransientCommandHandler>()
     .AddCommandHandler(p => new MyFactoryCommandHandler(), ServiceLifetime.Scoped)
     .AddQueryHandler<MySingletonQueryHandler>(ServiceLifetime.Singleton)
     .AddQueryHandler(new MySingletonQueryHandler());

    // register custom middlewares
    b.AddCommandMiddleware<MyTransientCommandMiddleware>()
     .AddCommandMiddleware(p => new MyFactoryCommandMiddleware(), ServiceLifetime.Scoped)
     .AddQueryMiddleware<MySingletonQueryMiddleware>(ServiceLifetime.Singleton)
     .AddQueryMiddleware(new MySingletonQueryMiddleware());
    
    // register pre-built middlewares
    b.AddLoggingMiddlewares();

    // register clients
    b.AddCommandClient<IMyExternalCommandHandler>(b => b.UseHttp(serverBaseAddress))
     .AddQueryClient<IMyExternalQueryHandler>(b => b.UseHttp(serverBaseAddress));

    // register remaining types
    b.AddTypesFromExecutingAssembly();
});

services.AddConquerorEventing((IConquerorEventingBuilder b) =>
{
    b.AddEventObserver<MyEventObserver>(ServiceLifetime.Singleton)
     .AddEventObserverMiddleware<MyEventObserverMiddleware>(ServiceLifetime.Singleton)
     .AddLoggingMiddlewares()
     .AddClient<IMyExternalEventObserver>(b => b.UseHttp(serverBaseAddress))
     .AddTypesFromExecutingAssembly();
});

// pros:
// - very explicit
// - safe due to generic constraints
// - need to create and maintain less overloads than 3b
// - consistent registration of clients and handlers
// - no need for "Conqueror", "ConquerorCQS" etc. as part of method names except "AddConqueror..."
// cons:
// - requires calling AddConquerorCQS every time
// - different API than normal registrations

// option 4a

services // IServiceCollection
        // register handlers
        .AddTransientConquerorCommandHandler<MyTransientCommandHandler>()
        .AddScopedConquerorCommandHandler(p => new MyFactoryCommandHandler())
        .AddSingletonConquerorQueryHandler<MySingletonQueryHandler>()
        .AddSingletonConquerorQueryHandler(new MySingletonQueryHandler())

        // register custom middlewares
        .AddTransientConquerorCommandMiddleware<MyTransientCommandMiddleware>()
        .AddTransientConquerorCommandMiddleware(p => new MyFactoryCommandMiddleware())
        .AddSingletonConquerorQueryMiddleware<MySingletonQueryMiddleware>()
        .AddSingletonConquerorQueryMiddleware(new MySingletonQueryMiddleware())

        // register pre-built middlewares
        .AddConquerorCQSLoggingMiddlewares()

        // register clients
        .AddConquerorCommandClient<IMyExternalCommandHandler>(b => b.UseHttp(serverBaseAddress))
        .AddConquerorQueryClient<IMyExternalQueryHandler>(b => b.UseHttp(serverBaseAddress))

        // register remaining types
        .AddConquerorCQSTypesFromExecutingAssembly();

services.AddTransientConquerorEventObserver<MyEventObserver>()
        .AddTransientConquerorEventObserverMiddleware<MyEventObserverMiddleware>()
        .AddConquerorEventingLoggingMiddlewares()
        .AddConquerorEventingClient<IMyExternalEventObserver>(b => b.UseHttp(serverBaseAddress))
        .AddConquerorEventingTypesFromExecutingAssembly();

// pros:
// - very explicit
// - somewhat similar to normal registrations
// - safe due to generic constraints
// - no need to call AddConquerorCQS at all
// - consistent registration of clients and handlers
// cons:
// - need to create and maintain lots of overloads
// - method names are very long and can make reading the code difficult
// - certain methods need "Conqueror", "ConquerorCQS", etc. as part of their name

// option 4b

services
        // register handlers
        .AddConquerorCommandHandler<MyTransientCommandHandler>()
        .AddConquerorCommandHandler(p => new MyFactoryCommandHandler(), ServiceLifetime.Scoped)
        .AddConquerorQueryHandler<MySingletonQueryHandler>(ServiceLifetime.Singleton)
        .AddConquerorQueryHandler(new MySingletonQueryHandler())

        // register custom middlewares
        .AddConquerorCommandMiddleware<MyTransientCommandMiddleware>()
        .AddConquerorCommandMiddleware(p => new MyFactoryCommandMiddleware(), ServiceLifetime.Scoped)
        .AddConquerorQueryMiddleware<MySingletonQueryMiddleware>(ServiceLifetime.Singleton)
        .AddConquerorQueryMiddleware(new MySingletonQueryMiddleware())

        // register pre-built middlewares
        .AddConquerorCQSLoggingMiddlewares()

        // register clients
        .AddConquerorCommandClient<IMyExternalCommandHandler>(b => b.UseHttp(serverBaseAddress))
        .AddConquerorQueryClient<IMyExternalQueryHandler>(b => b.UseHttp(serverBaseAddress))

        // register remaining types
        .AddConquerorCQSTypesFromExecutingAssembly();

services.AddConquerorEventObserver<MyEventObserver>(ServiceLifetime.Singleton)
        .AddConquerorEventObserverMiddleware<MyEventObserverMiddleware>(ServiceLifetime.Singleton)
        .AddConquerorEventingLoggingMiddlewares()
        .AddConquerorEventingClient<IMyExternalEventObserver>(b => b.UseHttp(serverBaseAddress))
        .AddConquerorEventingTypesFromExecutingAssembly();

// pros:
// - very explicit
// - consistent method names
// - safe due to generic constraints
// - no need to call AddConquerorCQS or AddConquerorEventing at all
// - need to create and maintain less overloads than 4a
// - code is less noisy than 3a
// cons:
// - method names are somewhat long and can make reading the code difficult

// option 4c

services
        // register handlers
        .AddCommandHandler<MyTransientCommandHandler>()
        .AddCommandHandler(p => new MyFactoryCommandHandler(), ServiceLifetime.Scoped)
        .AddQueryHandler<MySingletonQueryHandler>(ServiceLifetime.Singleton)
        .AddQueryHandler(new MySingletonQueryHandler())

        // register custom middlewares
        .AddCommandMiddleware<MyTransientCommandMiddleware>()
        .AddCommandMiddleware(p => new MyFactoryCommandMiddleware(), ServiceLifetime.Scoped)
        .AddQueryMiddleware<MySingletonQueryMiddleware>(ServiceLifetime.Singleton)
        .AddQueryMiddleware(new MySingletonQueryMiddleware())

        // register pre-built middlewares
        .AddConquerorCQSLoggingMiddlewares()

        // register clients
        .AddCommandClient<IMyExternalCommandHandler>(b => b.UseHttp(serverBaseAddress))
        .AddQueryClient<IMyExternalQueryHandler>(b => b.UseHttp(serverBaseAddress))

        // register remaining types
        .AddConquerorCQSTypesFromExecutingAssembly();

services.AddEventObserver<MyEventObserver>(ServiceLifetime.Singleton)
        .AddEventObserverMiddleware<MyEventObserverMiddleware>(ServiceLifetime.Singleton)
        .AddConquerorEventingLoggingMiddlewares()
        .AddEventingClient<IMyExternalEventObserver>(b => b.UseHttp(serverBaseAddress))
        .AddConquerorEventingTypesFromExecutingAssembly();

// pros:
// - very explicit
// - consistent method names
// - safe due to generic constraints
// - no need to call AddConquerorCQS or AddConquerorEventing at all
// - need to create and maintain less overloads than 4a
// - code is less noisy than 4a
// cons:
// - method names are somewhat long and can make reading the code difficult
```
