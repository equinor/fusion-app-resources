import { ReadonlyCollection } from '../../../../../../../reducers/utils';

export type DefaultTableType<T, TReturn> = {
    item: T;
    accessor: (item: T) => TReturn;
    onChange: (key: any, accessKey: keyof T, value: any) => void;
    accessKey: keyof T;
    rowIdentifier: keyof T;
    columnLabel?: string;
    isFetching?: boolean;
    componentState?: ReadonlyCollection<TReturn>
};
