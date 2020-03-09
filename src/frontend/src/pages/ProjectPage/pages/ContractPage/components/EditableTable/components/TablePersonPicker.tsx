import * as React from 'react';
import { PersonPicker } from '@equinor/fusion-components';
import { PersonDetails } from '@equinor/fusion';


export type TablePersonPickerProps<T> = {
    item: T;
    accessor: (item: T) => PersonDetails | null;
    onChange: (key: any, accessKey: keyof T, value: any) => void;
    accessKey: keyof T;
    rowIdentifier: keyof T;
    columnLabel: string;
};

function TablePersonPicker<T>({
    item,
    onChange,
    accessKey,
    accessor,
    rowIdentifier,
    columnLabel,
}: TablePersonPickerProps<T>) {
    const onPersonChange = React.useCallback(
        (person: PersonDetails) => {
            onChange(item[rowIdentifier], accessKey, person);
        },
        [onChange, item, accessKey, rowIdentifier]
    );
    return (
        <PersonPicker
            label={columnLabel}
            onSelect={onPersonChange}
            selectedPerson={accessor(item)}
        />
    );
}

export default TablePersonPicker;
