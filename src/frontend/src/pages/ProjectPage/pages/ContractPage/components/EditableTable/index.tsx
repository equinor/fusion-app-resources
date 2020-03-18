import * as React from 'react';
import * as styles from './styles.less';
import Taskbar from './Taskbar';
import TableTextInput from './components/TableTextInput';
import TableBasePosition from './components/TableBasePositionPicker';
import TablePositionPicker from './components/TablePositionPicker';
import TablePersonPicker from './components/TablePersonPicker';
import TablePersonnelPicker from './components/TablePersonnelPicker';
import TableDatePicker from './components/TableDatePicker';
import Personnel from '../../../../../../models/Personnel';
import { BasePosition } from '@equinor/fusion';
import { ReadonlyCollection } from '../../../../../../reducers/utils';
import TableTextEditor from './components/TableTextArea';
import SelectionCell from '../../pages/ManagePersonnelPage/components/SelectionCell';
import classNames from 'classnames';
import { useTooltipRef } from '@equinor/fusion-components';
import TableToolbar from './components/TableToolbar';

export type EditableTableComponentState = {
    personnel?: ReadonlyCollection<Personnel>;
    basePositions?: ReadonlyCollection<BasePosition>;
};

export type EditableTaleColumnItem =
    | 'TextInput'
    | 'PersonPicker'
    | 'PositionPicker'
    | 'BasePositionPicker'
    | 'PersonnelPicker'
    | 'DatePicker'
    | 'TextArea';

export type EditableTaleColumn<T> = {
    accessor: (item: T) => any;
    item: EditableTaleColumnItem;
    label: string;
    accessKey: keyof T;
};

type EditableTableProps<T> = {
    formState: T[];
    setFormState: (newState: T[]) => void;
    columns: EditableTaleColumn<T>[];
    createDefaultState: () => T[];
    rowIdentifier: keyof T;
    isFetching?: boolean;
    componentState?: EditableTableComponentState;
};

function EditableTable<T>({
    formState,
    setFormState,
    columns,
    createDefaultState,
    rowIdentifier,
    isFetching,
    componentState,
}: EditableTableProps<T>) {
    const [selectedItems, setSelectedItems] = React.useState<T[]>([]);

    const onChange = (key: any, accessKey: keyof T, value: any) => {
        const updatedPersons = [...formState].map(stateItem =>
            stateItem[rowIdentifier] === key ? { ...stateItem, [accessKey]: value } : stateItem
        );
        setFormState(updatedPersons);
    };

    const onAddItem = React.useCallback(() => {
        const newStateItem = createDefaultState();
        setFormState([...formState, ...newStateItem]);
    }, [formState, createDefaultState]);

    const onRemoveItems = React.useCallback(
        (items: T[]) => {
            const newFormState = formState.filter(
                stateItem => !items.some(item => item[rowIdentifier] === stateItem[rowIdentifier])
            );
            const newSelectedItems = selectedItems.filter(
                stateItem => !items.some(item => item[rowIdentifier] === stateItem[rowIdentifier])
            );
            setFormState(newFormState);
            setSelectedItems(newSelectedItems);
        },
        [formState, rowIdentifier, selectedItems]
    );

    React.useEffect(() => {
        if (formState && formState.length <= 0) {
            onAddItem();
        }
    }, [formState]);

    const getTableComponent = React.useCallback(
        (column: EditableTaleColumn<T>, item: T) => {
            const defaultProps = {
                item,
                accessKey: column.accessKey,
                accessor: column.accessor,
                onChange,
                rowIdentifier,
                isFetching,
                columnLabel: column.label,
            };
            switch (column.item) {
                case 'TextInput':
                    return <TableTextInput {...defaultProps} />;
                case 'BasePositionPicker':
                    return (
                        <TableBasePosition
                            {...defaultProps}
                            componentState={componentState?.basePositions}
                        />
                    );
                case 'PositionPicker':
                    return <TablePositionPicker {...defaultProps} />;
                case 'PersonPicker':
                    return <TablePersonPicker {...defaultProps} />;
                case 'PersonnelPicker':
                    return (
                        <TablePersonnelPicker
                            {...defaultProps}
                            componentState={componentState?.personnel}
                        />
                    );
                case 'DatePicker':
                    return <TableDatePicker {...defaultProps} />;
                case 'TextArea':
                    return <TableTextEditor {...defaultProps} />;
                default:
                    return null;
            }
        },
        [onChange, rowIdentifier, onChange, rowIdentifier, isFetching]
    );

    const onItemSelectChange = React.useCallback(
        (item: T) => {
            if (selectedItems && selectedItems.some(i => i === item)) {
                setSelectedItems(selectedItems.filter(i => i !== item));
            } else {
                setSelectedItems([...(selectedItems || []), item]);
            }
        },
        [selectedItems]
    );

    const isAllSelected = React.useMemo(() => selectedItems.length === formState.length, [
        selectedItems,
        formState,
    ]);

    const selectableTooltipRef = useTooltipRef(
        isAllSelected ? 'Unselect all' : 'Select all',
        'above'
    );

    const onSelectAll = React.useCallback(() => {
        setSelectedItems(selectedItems.length === formState.length ? [] : formState);
    }, [formState, selectedItems]);

    const tableHeader = React.useMemo(() => {
        return (
            <thead>
                <tr>
                    <th
                        className={classNames(styles.header, styles.selectionCell)}
                        ref={selectableTooltipRef}
                    >
                        <SelectionCell
                            isSelected={
                                !!selectedItems && selectedItems.length === formState.length
                            }
                            onChange={onSelectAll}
                            indeterminate={
                                !!selectedItems &&
                                selectedItems.length > 0 &&
                                selectedItems.length !== formState.length
                            }
                        />
                    </th>
                    <th className={classNames(styles.header, styles.toolbarCell)} />

                    {columns.map(column => (
                        <th className={styles.header} key={column.label + 'header'}>
                            {column.label}
                        </th>
                    ))}
                </tr>
            </thead>
        );
    }, [columns, selectedItems, formState]);

    const getRowBodySelectedState = React.useCallback(
        (stateItem: T) =>
            !!selectedItems &&
            selectedItems.some(
                selectedItem => selectedItem[rowIdentifier] === stateItem[rowIdentifier]
            ),
        [selectedItems]
    );

    const tableBody = React.useMemo(() => {
        return (
            <tbody>
                {formState.map((stateItem, index) => (
                    <tr key={`item-${index}`}>
                        <td className={classNames(styles.tableRowCell, styles.selectionCell)}>
                            <SelectionCell
                                isSelected={getRowBodySelectedState(stateItem)}
                                onChange={() => onItemSelectChange(stateItem)}
                            />
                        </td>
                        <td className={classNames(styles.tableRowCell, styles.toolbarCell)}>
                            <TableToolbar onRemove={() => onRemoveItems([stateItem])} />
                        </td>
                        {columns.map(column => (
                            <td key={column.accessKey.toString()} className={styles.tableRowCell}>
                                {getTableComponent(column, stateItem)}
                            </td>
                        ))}
                    </tr>
                ))}
            </tbody>
        );
    }, [columns, formState, selectedItems]);

    return (
        <div className={styles.editableTable}>
            <Taskbar
                onAddItem={onAddItem}
                selectedItems={selectedItems}
                onRemoveItem={onRemoveItems}
            />
            <div className={styles.container}>
                <table className={styles.table}>
                    {tableHeader}
                    {tableBody}
                </table>
            </div>
        </div>
    );
}

export default EditableTable;
