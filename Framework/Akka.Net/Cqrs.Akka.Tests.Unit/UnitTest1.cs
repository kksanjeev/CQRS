﻿using System;
using System.Threading;
using cdmdotnet.Logging;
using cdmdotnet.Logging.Configuration;
using Cqrs.Akka.Commands;
using Cqrs.Akka.Domain;
using Cqrs.Akka.Events;
using Cqrs.Akka.Tests.Unit.Commands;
using Cqrs.Akka.Tests.Unit.Commands.Handlers;
using Cqrs.Akka.Tests.Unit.Events;
using Cqrs.Akka.Tests.Unit.Events.Handlers;
using Cqrs.Bus;
using Cqrs.Configuration;
using Cqrs.Domain;
using Cqrs.Domain.Factories;
using Cqrs.Events;
using Cqrs.Ninject.Akka;
using Cqrs.Ninject.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ninject;

namespace Cqrs.Akka.Tests.Unit
{
	[TestClass]
	public class UnitTest1
	{
		[TestMethod]
		public void TestMethod1()
		{
			// Arrange
			var command = new SayHelloWorldCommand();
			ICorrelationIdHelper correlationIdHelper = new NullCorrelationIdHelper();
			ILogger logger = new ConsoleLogger(new LoggerSettings(), correlationIdHelper);
			IConfigurationManager configurationManager = new ConfigurationManager();
			IBusHelper busHelper = new BusHelper(configurationManager);
			var inProcessBus = new InProcessBus<Guid>(null, correlationIdHelper, null, logger, configurationManager, busHelper);
			var commandBus = new AkkaCommandBus<Guid>(busHelper, null, correlationIdHelper, null, logger, inProcessBus, null);
			var eventBus = new AkkaEventBus<Guid>(busHelper, null, correlationIdHelper, logger, inProcessBus, null);

			var kernel = new StandardKernel();
			kernel.Bind<ILogger>().ToConstant(logger);
			kernel.Bind<IAggregateFactory>().To<AggregateFactory>();
			kernel.Bind<IUnitOfWork<Guid>>().To<UnitOfWork<Guid>>();
			kernel.Bind<IRepository<Guid>>().To<AkkaRepository<Guid>>();
			kernel.Bind<IAkkaRepository<Guid>>().To<AkkaRepository<Guid>>();
			kernel.Bind<IEventStore<Guid>>().To<MemoryCacheEventStore<Guid>>();
			kernel.Bind<IEventBuilder<Guid>>().To<DefaultEventBuilder<Guid>>();
			kernel.Bind<IEventDeserialiser<Guid>>().To<EventDeserialiser<Guid>>();
			kernel.Bind<IEventPublisher<Guid>>().ToConstant(inProcessBus);
			kernel.Bind<ICorrelationIdHelper>().ToConstant(correlationIdHelper);
			kernel.Bind<IAkkaEventBus<Guid>>().ToConstant(eventBus);

			AkkaNinjectDependencyResolver.Start(kernel);
			var dependencyResolver = NinjectDependencyResolver.Current as AkkaNinjectDependencyResolver;


			// Commands handled by Akka.net
			commandBus.RegisterHandler<SayHelloWorldCommand>(new SayHelloWorldCommandHandler(dependencyResolver).Handle);
			commandBus.RegisterHandler<HelloWorldReplyCommand>(new HelloWorldReplyCommandHandler(dependencyResolver).Handle);

			// Events
			inProcessBus.RegisterHandler<HelloWorldSaid>(new HelloWorldSaidEventHandler(commandBus).Handle);

			// Act
			commandBus.Send(command);

			// Assert
			SpinWait.SpinUntil(() => false);
		}
	}
}
