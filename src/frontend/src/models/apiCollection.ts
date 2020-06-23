type ApiCollection<T> = {
    value: T[];
};

export type ApiCollectionRequest<T> = {
    code: string;
    message: string;
    value: T;
};

export default ApiCollection;
