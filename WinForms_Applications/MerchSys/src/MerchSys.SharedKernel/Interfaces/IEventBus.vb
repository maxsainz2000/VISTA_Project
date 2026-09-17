Imports MediatR
Imports System.Threading

Namespace Interfaces

    ''' <summary>
    ''' Optional abstraction over IMediator for publishing domain events.
    ''' Allows module code to depend on this narrower interface instead of the full IMediator,
    ''' keeping the intent of "fire an event" distinct from "send a request".
    ''' </summary>
    Public Interface IEventBus

        ''' <summary>Publish a domain event to all registered notification handlers.</summary>
        ''' <param name="notification">The event to publish.</param>
        ''' <param name="cancellationToken">Optional cancellation token.</param>
        Function PublishAsync(notification As INotification, Optional cancellationToken As CancellationToken = Nothing) As Task

    End Interface

End Namespace
