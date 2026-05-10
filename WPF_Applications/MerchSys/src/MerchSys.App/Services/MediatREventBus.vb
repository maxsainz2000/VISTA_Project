Imports MediatR
Imports MerchSys.SharedKernel.Interfaces
Imports System.Threading

Namespace Services

    ''' <summary>
    ''' Thin adapter that delegates IEventBus.PublishAsync to MediatR's IMediator.Publish.
    ''' Keeps module code depending on the narrower IEventBus interface rather than the full IMediator.
    ''' </summary>
    Public Class MediatREventBus
        Implements IEventBus

        Private ReadOnly _mediator As IMediator

        Public Sub New(mediator As IMediator)
            _mediator = mediator
        End Sub

        Public Function PublishAsync(notification As INotification,
                                     Optional cancellationToken As CancellationToken = Nothing) As Task _
            Implements IEventBus.PublishAsync
            Return _mediator.Publish(notification, cancellationToken)
        End Function

    End Class

End Namespace
