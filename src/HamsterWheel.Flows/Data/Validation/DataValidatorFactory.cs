using System.Linq.Expressions;

namespace HamsterWheel.Flows.Data.Validation;

public class DataValidatorFactory : IDataValidatorFactory
{
    public IDataValidator<T> For<T>() => new DataValidator<T>();

    public IDataValidator<T> For<T>(T obj) => new DataValidator<T>();
}

public class DataValidator<T> : IDataValidator<T>
{
    private readonly Dictionary<Expression, Func<T, bool>> _properties = new();

    public IPropertyValidationContext<TProp> Property<TProp>(Expression<Func<T, TProp>> propertySelector)
    {
        return new PropertyValidationContext<T, TProp>(this, propertySelector);
    }

    public void AddPropertyValidation(Expression propertySelector, Func<T, bool> predicate)
    {
        _properties.Add(propertySelector, predicate);
    }
}

public class PropertyValidationContext<T, TProp>(DataValidator<T> validator, Expression<Func<T, TProp>> propSelector)
    : IPropertyValidationContext<TProp>
{
    public void Must(Func<TProp, bool> check)
    {
        validator.AddPropertyValidation(propSelector, o => check(propSelector.Compile()(o)));
    }
}