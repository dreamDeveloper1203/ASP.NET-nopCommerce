﻿using System.Threading.Tasks;

namespace Nop.Services.Events
{
    /// <summary>
    /// Represents an event publisher
    /// </summary>
    public partial interface IEventPublisher
    {
        /// <summary>
        /// Publish event to consumers
        /// </summary>
        /// <typeparam name="TEvent">Type of event</typeparam>
        /// <param name="event">Event object</param>
        Task Publish<TEvent>(TEvent @event);
    }
}