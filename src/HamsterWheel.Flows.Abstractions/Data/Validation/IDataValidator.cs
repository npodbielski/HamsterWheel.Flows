using System.Linq.Expressions;

namespace HamsterWheel.Flows.Data.Validation;

public interface IDataValidator<T>
{
    IPropertyValidationContext<TProp> Property<TProp>(Expression<Func<T, TProp>> propertySelector);
}

public interface IPropertyValidationContext<out T>
{
    void Must(Func<T, bool> check);
}

public interface IDataValidatorFactory
{
    IDataValidator<T> For<T>(); 
    IDataValidator<T> For<T>(T obj); 
}

