using System.Linq;

namespace StellarChecker
{
    /// <summary>
    /// Линия задержки
    /// </summary>
    /// <typeparam name="TDataType"></typeparam>
    class DelayLine<TDataType>
    {
        /// <summary>
        /// Преобразование двух значений точек съёма
        /// </summary>
        /// <param name="op1">Точка съёма 1</param>
        /// <param name="op2">Точка съёма 2</param>
        /// <returns>Результат преобразования</returns>
        public delegate TDataType SomeAction(TDataType op1, TDataType op2);
        
        /// <summary>
        /// Заполнение регистра
        /// </summary>
        public TDataType[] Filling
        {
            get { return _filling; }
            set
            {
                _filling = value;
                _register.SetFilling(value);
            }
        }
        public readonly int[] RemovalPointIndexes;
        private TDataType[] _filling;
        private readonly ShuffleRegister<TDataType> _register;
        protected readonly SomeAction Action;
        
        /// <summary>
        /// Линия задержки
        /// </summary>
        /// <param name="removalPointIndexes">Индексы точек съёма</param>
        /// <param name="filling">Начальное заполнение</param>
        /// <param name="action">Преобразование значений точек съёма</param>
        public DelayLine(int[] removalPointIndexes, TDataType[] filling, SomeAction action)
        {
            RemovalPointIndexes = removalPointIndexes;
            _register = new ShuffleRegister<TDataType>(removalPointIndexes.Max()+1);
            Filling = filling;
            Action = action;
        }

        /// <summary>
        /// Линия задержки
        /// </summary>
        /// <param name="removalPointIndexes">Индексы точек съёма</param>
        /// <param name="action">Преобразование значений точек съёма</param>
        public DelayLine(int[] removalPointIndexes, SomeAction action)
        {
            RemovalPointIndexes = removalPointIndexes;
            _register = new ShuffleRegister<TDataType>(removalPointIndexes.Max() + 1);
            //not safe
            Filling = new[] { default(TDataType) };
            Action = action;
        }


        /// <summary>
        /// Получить один отсчёт
        /// </summary>
        /// <returns>Один отсчёт</returns>
        public TDataType GetCount()
        {
            TDataType value = default(TDataType);
            value = RemovalPointIndexes.Aggregate(value, (current, removalPointIndex) => Action(current, _register.GetcountByIndex(removalPointIndex)));
            _register.AppendCountToHead(value);
            return value;
        }
    }
}
