import { useCallback } from 'react';
import { DefaultTableType } from './TableTypes';
import ResourcesDatePicker from '../../../../../../../components/ResourcesDatePicker';

function TableDatePicker<T>({
    item,
    onChange,
    accessKey,
    accessor,
    rowIdentifier,
}: DefaultTableType<T, Date>) {
    const onDateChange = useCallback(
        (newDate: Date | null) => {
            onChange(item[rowIdentifier], accessKey, newDate);
        },
        [onChange, item, accessKey, rowIdentifier]
    );
    return <ResourcesDatePicker onChange={onDateChange} date={accessor(item)} />;
}

export default TableDatePicker;
