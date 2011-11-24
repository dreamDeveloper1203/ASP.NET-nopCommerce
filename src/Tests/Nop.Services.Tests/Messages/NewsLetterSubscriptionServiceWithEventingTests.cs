﻿using NUnit.Framework;
using Nop.Core.Data;
using Nop.Core.Domain.Messages;
using Nop.Core.Events;
using Nop.Services.Messages;
using Rhino.Mocks;

namespace Nop.Services.Tests.Messages {
    [TestFixture]
    public class NewsLetterSubscriptionServiceWithEventingTests {
        /// <summary>
        /// Verifies the active insert triggers subscribe event.
        /// </summary>
        [Test]
        public void VerifyActiveInsertTriggersSubscribeEvent() {
            var eventPublisher = MockRepository.GenerateStub<IEventPublisher>();
            var repo = MockRepository.GenerateStub<IRepository<NewsLetterSubscription>>();

            var subscription = new NewsLetterSubscription {Active = true, Email = "skyler@csharpwebdeveloper.com"};

            var service = new NewsLetterSubscriptionServiceWithEventing(repo, eventPublisher);
            service.InsertNewsLetterSubscription(subscription, true);

            eventPublisher.AssertWasCalled(x => x.Publish(new EmailSubscribed(subscription.Email)));
        }

        /// <summary>
        /// Verifies the delete triggers unsubscribe event.
        /// </summary>
        [Test]
        public void VerifyDeleteTriggersUnsubscribeEvent() {
            var eventPublisher = MockRepository.GenerateStub<IEventPublisher>();
            var repo = MockRepository.GenerateStub<IRepository<NewsLetterSubscription>>();

            var subscription = new NewsLetterSubscription {Active = true, Email = "skyler@csharpwebdeveloper.com"};

            var service = new NewsLetterSubscriptionServiceWithEventing(repo, eventPublisher);
            service.DeleteNewsLetterSubscription(subscription, true);

            eventPublisher.AssertWasCalled(x => x.Publish(new EmailUnsubscribed(subscription.Email)));
        }

        /// <summary>
        /// Verifies the email update triggers unsubscribe and subscribe event.
        /// </summary>
        [Test]
        public void VerifyEmailUpdateTriggersUnsubscribeAndSubscribeEvent() {
            var eventPublisher = MockRepository.GenerateStub<IEventPublisher>();
            var repo = MockRepository.GenerateStub<IRepository<NewsLetterSubscription>>();

            //Prepare the original result
            var originalSubscription = new NewsLetterSubscription {Active = true, Email = "skyler@csharpwebdeveloper.com"};
            repo.Stub(m => m.GetById(Arg<object>.Is.Anything)).Return(originalSubscription);

            var subscription = new NewsLetterSubscription {Active = true, Email = "skyler@tetragensoftware.com"};

            var service = new NewsLetterSubscriptionServiceWithEventing(repo, eventPublisher);
            service.UpdateNewsLetterSubscription(subscription, true);

            eventPublisher.AssertWasCalled(x => x.Publish(new EmailUnsubscribed(originalSubscription.Email)));
            eventPublisher.AssertWasCalled(x => x.Publish(new EmailSubscribed(subscription.Email)));
        }

        /// <summary>
        /// Verifies the inactive to active update triggers subscribe event.
        /// </summary>
        [Test]
        public void VerifyInactiveToActiveUpdateTriggersSubscribeEvent() {
            var eventPublisher = MockRepository.GenerateStub<IEventPublisher>();
            var repo = MockRepository.GenerateStub<IRepository<NewsLetterSubscription>>();

            //Prepare the original result
            var originalSubscription = new NewsLetterSubscription {Active = false, Email = "skyler@csharpwebdeveloper.com"};
            repo.Stub(m => m.GetById(Arg<object>.Is.Anything)).Return(originalSubscription);

            var subscription = new NewsLetterSubscription {Active = true, Email = "skyler@csharpwebdeveloper.com"};

            var service = new NewsLetterSubscriptionServiceWithEventing(repo, eventPublisher);
            service.UpdateNewsLetterSubscription(subscription, true);

            eventPublisher.AssertWasCalled(x => x.Publish(new EmailSubscribed(subscription.Email)));
        }

        /// <summary>
        /// Verifies the insert event is fired.
        /// </summary>
        [Test]
        public void VerifyInsertEventIsFired() {
            var eventPublisher = MockRepository.GenerateStub<IEventPublisher>();
            var repo = MockRepository.GenerateStub<IRepository<NewsLetterSubscription>>();

            var service = new NewsLetterSubscriptionServiceWithEventing(repo, eventPublisher);
            service.InsertNewsLetterSubscription(new NewsLetterSubscription {Email = "skyler@csharpwebdeveloper.com"});

            eventPublisher.AssertWasCalled(x => x.EntityInserted(Arg<NewsLetterSubscription>.Is.Anything));
        }

        /// <summary>
        /// Verifies the update event is fired.
        /// </summary>
        [Test]
        public void VerifyUpdateEventIsFired() {
            var eventPublisher = MockRepository.GenerateStub<IEventPublisher>();
            var repo = MockRepository.GenerateStub<IRepository<NewsLetterSubscription>>();

            //Prepare the original result
            var originalSubscription = new NewsLetterSubscription {Active = false, Email = "skyler@csharpwebdeveloper.com"};
            repo.Stub(m => m.GetById(Arg<object>.Is.Anything)).Return(originalSubscription);

            var service = new NewsLetterSubscriptionServiceWithEventing(repo, eventPublisher);
            service.UpdateNewsLetterSubscription(new NewsLetterSubscription {Email = "skyler@csharpwebdeveloper.com"});

            eventPublisher.AssertWasCalled(x => x.EntityUpdated(Arg<NewsLetterSubscription>.Is.Anything));
        }
    }
}