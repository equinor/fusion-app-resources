import { EditableTaleColumns, EditableTaleColumnItem } from '.';
import { DataTableColumn, TextInput } from '@equinor/fusion-components';
import * as React from 'react';

type ColumnProps<T> = {
    item: T;
    accessor: (item: T) => string;
    onChange: (key: any, accessKey: keyof T, value: any) => void;
    accessKey: keyof T;
    rowIdentifier: keyof T;
};

function TextInputColumn<T>({
    item,
    accessor,
    onChange,
    accessKey,
    rowIdentifier,
}: ColumnProps<T>) {
    const onInputChange = React.useCallback(
        (newValue: string) => {
            onChange(item[rowIdentifier], accessKey, newValue);
        },
        [onChange, item, accessKey, rowIdentifier]
    );
    return <TextInput value={accessor(item)} onChange={onInputChange} />;
}

export default <T extends {}>(
    columns: EditableTaleColumns<T>[],
    onChange: (key: any, accessKey: keyof T, value: any) => void,
    rowIdentifier: keyof T
): DataTableColumn<T>[] => {
    const getColumnComponent = React.useCallback(
        (
            itemType: EditableTaleColumnItem,
            item: T,
            accessor: (item: T) => string,
            key: keyof T
        ) => {
            switch (itemType) {
                case 'TextInput':
                    return (
                        <TextInputColumn
                            item={item}
                            accessor={accessor}
                            onChange={onChange}
                            accessKey={key}
                            rowIdentifier={rowIdentifier}
                        />
                    );
                default:
                    return null;
            }
        },
        [onChange, rowIdentifier]
    );

    const editColumns = React.useMemo(
        (): DataTableColumn<T>[] =>
            columns.map(column => ({
                accessor: item => column.accessor(item),
                key: column.accessKey as string,
                label: column.label,
                component: rowItem =>
                    getColumnComponent(
                        column.item,
                        rowItem.item,
                        column.accessor,
                        column.accessKey
                    ),
            })),
        [columns]
    );

    return editColumns;
};
