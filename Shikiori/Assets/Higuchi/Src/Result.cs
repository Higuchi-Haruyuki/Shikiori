
using UnityEngine;
namespace ErrorHandling
{
    /// <summary>
    /// 結果を表すクラス。成功か失敗かを示すフラグと、成功時の値、失敗時のエラーメッセージを保持する。
    /// </summary>
    /// <typeparam name="T">扱う型</typeparam>
    public class Result<T>
    {
        public bool IsSuccess { get; private set; } = false;
        public T Value { get; private set; }
        public string ErrorMessage { get; private set; }

        private Result(bool isSuccess, T value, string errorMessage)
        {
            IsSuccess = isSuccess;
            Value = value;
            ErrorMessage = errorMessage;
        }

        /// <summary>
        /// 成功の結果を作成する
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static Result<T> Success(T value)
        {
            return new Result<T>(true, value, null);
        }

        /// <summary>
        /// 失敗の結果を作成する
        /// </summary>
        /// <param name="errorMessage"></param>
        /// <returns></returns>
        public static Result<T> Failure(string errorMessage)
        {
            return new Result<T>(false, default(T), errorMessage);
        }
    }
}
