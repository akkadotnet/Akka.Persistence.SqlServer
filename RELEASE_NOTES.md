#### 1.4.10 August 21 2020 ####
* Upgrades to Akka.NET v1.4.10
* Resolves high memory consumption issues with the `SqlJournal` as a result of https://github.com/akkadotnet/akka.net/issues/4524
* Adds the new `AllEventsQuery` to the `IReadJournal` for [Akka.Persistence.Query support for SQL Server](https://getakka.net/articles/persistence/persistence-query.html#predefined-queries).
