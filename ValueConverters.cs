using System;
using System.Globalization;

namespace DataBinding
{
  public interface ValueConverter
  {
    object Convert( object value, Type targetType, object parameter, CultureInfo ci );

    object ConvertBack( object value, Type targetType, object parameter, CultureInfo ci );
  }

  public class IdentityConverter : ValueConverter
  {
    public object Convert( object value, Type targetType, object parameter, CultureInfo ci )
    {
      return value;
    }

    public object ConvertBack( object value, Type targetType, object parameter, CultureInfo ci )
    {
      return value;
    }
  }

  public class StringFormatConverter : ValueConverter
  {
    public object Convert( object value, Type targetType, object parameter, CultureInfo ci )
    {
      if ( value == null )
      {
        return "---";
      }
      var format = ( parameter as string ) ?? Format;
      if ( format == null )
      {
        return value;
      }
      return String.Format( ci, format, value );
    }

    public object ConvertBack( object value, Type targetType, object parameter, CultureInfo ci )
    {
      return null;
    }

    public string Format { get; set; }
  }

  public class InverseBooleanConverter : ValueConverter
  {
    public object Convert( object value, Type targetType, object parameter, CultureInfo culture )
    {
      return !( bool ) value;
    }
    public object ConvertBack( object value, Type targetType, object parameter, CultureInfo culture )
    {
      return !( bool ) value;
    }
  }

  public class DoubleToIntegerConverter : ValueConverter
  {
    public object Convert( object value, Type targetType, object parameter, CultureInfo culture )
    {
      return System.Convert.ToDouble( value );
    }
    public object ConvertBack( object value, Type targetType, object parameter, CultureInfo culture )
    {
      return System.Convert.ToInt32( value );
    }
  }

  public class EnumToIntConverter : ValueConverter
  {
    public object Convert( object value, Type targetType, object parameter, CultureInfo culture )
    {
      int returnValue = 0;
      if ( parameter is Type )
      {
        returnValue = ( int ) value;
      }
      return returnValue;
    }

    public object ConvertBack( object value, Type targetType, object parameter, CultureInfo culture )
    {
      Enum enumValue = default( Enum );
      if ( parameter is Type )
      {
        enumValue = ( Enum ) Enum.Parse( ( Type ) parameter, value.ToString() );
      }
      return enumValue;
    }
  }
}
