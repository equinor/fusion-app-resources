import { useCallback } from 'react';
import { DatePicker} from '@equinor/fusion-components';
import { DefaultTableType } from './TableTypes';

function TableDatePicker<T>({
    item,
    onChange,
    accessKey,
    accessor,
    rowIdentifier,
    columnLabel,
}: DefaultTableType<T, Date>) {
    const onDateChange = useCallback(
        (newDate: Date | null) => {
            onChange(item[rowIdentifier], accessKey, newDate);
        },
        [onChange, item, accessKey, rowIdentifier]
    );
    return <DatePicker onChange={onDateChange} selectedDate={accessor(item)} />;
}

export default TableDatePicker;
