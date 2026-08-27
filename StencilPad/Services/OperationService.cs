using Microsoft.Extensions.Logging;
using StencilPad.Models;
using StencilPad.Models.Operations;

namespace StencilPad.Services;

public class OperationService : IOperationService
{
    private class DummyContext : IDisposable
    {
        public void Dispose()
        {
            // ...
        }
    }
    
    private static readonly IDisposable DummyContextInstance = new DummyContext();
    
    public bool HasEditContext => _currentEditContext is not null;

    private readonly ILogger<OperationService> _logger;
    private IEditContext? _currentEditContext;
    
    public event Action<IOperation, bool>? OperationPushed;

    public OperationService(ILogger<OperationService> logger)
    {
        _logger = logger;
        _currentEditContext = null;
    }
    
    public IDisposable CreateEditContext(Sheet sheet,
                                         IEnumerable<ISheetElement> elements)
    {
        if (_currentEditContext is not null)
        {
            // This is a warning rather than an error because a nested context
            // is a bug and an annoyance that will lose undo steps, but throwing
            // an exception would be outright disruptive to the user, possibly
            // losing work.
            _logger.LogWarning("Trying to create a new edit context while another one is active");
        }

        return TryCreateEditContext(sheet, elements);
    }
    
    public IDisposable CreateEditContext(Sheet sheet,
                                         ISheetElement element)
    {
        if (_currentEditContext is not null)
        {
            _logger.LogWarning("Trying to create a new edit context while another one is active");
        }

        return TryCreateEditContext(sheet, element);
    }

    // These methods are used when we may or may not be in an edit context
    // depending on a widget's internal state, such as dragging vs manual entry,
    // so they fail gracefully.
    public IDisposable TryCreateEditContext(Sheet sheet,
                                            IEnumerable<ISheetElement> elements)
    {
        if (_currentEditContext is not null)
        {
            return DummyContextInstance;
        }

        _currentEditContext = new EditSheetElementContext(sheet, elements, this);

        return _currentEditContext;
    }
    
    public IDisposable TryCreateEditContext(Sheet sheet,
                                            ISheetElement element)
    {
        if (_currentEditContext is not null)
        {
            return DummyContextInstance;
        }

        _currentEditContext = new EditSheetElementContext(sheet, element, this);

        return _currentEditContext;
    }

    public void FlushEditContext()
    {
        if (_currentEditContext is null)
        {
            return;
        }

        // Generally this is a failover so that a tool doesn't have any dangling
        // context when it gets switched out, so flag it.
        _logger.LogWarning("Flushing edit context");
        
        _currentEditContext.Dispose();
        _currentEditContext = null;
    }

    public void DiscardEditContext()
    {
        if (_currentEditContext is null)
        {
            return;
        }

        // Also a failover.
        _logger.LogWarning("Discarding edit context");

        _currentEditContext.Discard();
        _currentEditContext = null;
    }
    
    public void Push(ICommandOperation? operation)
    {
        if (operation is null)
        {
            return;
        }
        
        OperationPushed?.Invoke(operation, true);
    }

    public void Flush(IEditContext editContext, IMementoOperation? operation)
    {
        if (_currentEditContext != editContext)
        {
            _logger.LogWarning("Flushed edit context does not match current context");
            return;
        }

        if (operation is not null)
        {
            Push(operation);
        }

        _currentEditContext = null;
    }

    private void Push(IMementoOperation? operation)
    {
        if (operation is null)
        {
            return;
        }
        
        OperationPushed?.Invoke(operation, false);
    }
}
