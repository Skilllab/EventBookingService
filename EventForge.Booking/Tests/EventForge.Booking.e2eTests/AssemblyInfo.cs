

// Принудительно заставляет Visual Studio запускать разные классы параллельно

using Xunit;

[assembly: CollectionBehavior(CollectionBehavior.CollectionPerClass, MaxParallelThreads = 4)]
