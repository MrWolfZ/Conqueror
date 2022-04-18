using System;

namespace Conqueror.CQS
{
    [AttributeUsage(AttributeTargets.Method)]
    public abstract class CommandMiddlewareConfigurationAttribute : Attribute
    {
    }
}
