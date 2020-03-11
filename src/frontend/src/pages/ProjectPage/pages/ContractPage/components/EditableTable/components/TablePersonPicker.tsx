import * as React from 'react';
import { PersonPicker } from '@equinor/fusion-components';
import { PersonDetails } from '@equinor/fusion';
import { DefaultTableType } from './TableTypes';

function TablePersonPicker<T>({
    item,
    onChange,
    accessKey,
    accessor,
    rowIdentifier,
    columnLabel,
}: DefaultTableType<T, PersonDetails | null>) {
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
