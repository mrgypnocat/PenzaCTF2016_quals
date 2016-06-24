using System;
using System.Linq;

namespace StellarChecker
{
    /// <summary>
    /// Регистр сдвига
    /// </summary>
    class ShuffleRegister<TDataType>
    {
        public readonly int Size;
        private TDataType[] _Counts;
        private int _headCountIndex;
        private int _tailCountIndex;

        /// <summary>
        /// Регистр сдвига
        /// </summary>
        /// <param name="size">Количество элементов</param>
        public ShuffleRegister(int size)
        {
            Size = size;
            _Counts = new TDataType[size];
            _headCountIndex = 0;
            _tailCountIndex = size - 1;
        }

        /// <summary>
        /// Задать начальное заполнение
        /// </summary>
        /// <param name="values">Набор значений для последовательного заполнения ячеек</param>
        /// <param name="initPosition">Позиция, с которой начинается заполнение</param>
        public void SetFilling(TDataType[] values, int initPosition = 0)
        {
            if (values!=null)
            {
                var min = values.Count() < Size ? values.Count() : Size;
                for (int i = 0; i < min; i++)
                {
                    _Counts[i] = values[i + initPosition];
                }
            }
        }

        /// <summary>
        /// Добавить значение в конец
        /// </summary>
        /// <param name="count">Отсчёт</param>
        public void AppendCountToTail(TDataType count)
        {
            try
            {
                _Counts[_headCountIndex] = count;
            }
            catch (Exception ex)
            {
                Console.WriteLine(@"{0} at {1}", ex.Message, ex.Source);
                return;
            }

            _headCountIndex++;
            _tailCountIndex++;
            if (_tailCountIndex == Size)
            {
                _tailCountIndex -= Size;
            }
            if (_headCountIndex == Size)
            {
                _headCountIndex -= Size;
            }
        }

        /// <summary>
        /// Добавить отсчёты в начало
        /// </summary>
        /// <param name="count">Отсчёт</param>
        public void AppendCountToHead(TDataType count)
        {
            try
            {
                _Counts[_tailCountIndex] = count;
            }
            catch (Exception ex)
            {
                Console.WriteLine(@"{0} at {1}", ex.Message, ex.Source);
                return;
            }

            _headCountIndex = _tailCountIndex;
            _tailCountIndex--;
            if (_tailCountIndex < 0)
            {
                _tailCountIndex += Size;
            }
        }

        /// <summary>
        /// Получить отсчёт по индексу ячейки, как элемент массива
        /// </summary>
        /// <param name="index">Индекс ячейки</param>
        /// <returns>Значение ячейки по данному индексу</returns>
        public TDataType GetcountByIndex(int index)
        {
            while (index / Size > 1)
            {
                index -= Size;
            }
            var realMassIndex = _headCountIndex + index;
            if (realMassIndex >= Size)
            {
                realMassIndex -= Size;
            }
            if (realMassIndex < 0)
            {
                realMassIndex += Size;
            }
            return _Counts[realMassIndex];
        }

        /// <summary>
        /// Получить содержимое регистра сдвига как массив
        /// </summary>
        /// <returns>Массив значений ячеек последовательно</returns>
        public TDataType[] GetCountsAsArray()
        {
            var result = new TDataType[Size];
            for (int i = 0; i < Size; i++)
            {
                result[i] = GetcountByIndex(i);
            }
            return result;
        }
    }
}
