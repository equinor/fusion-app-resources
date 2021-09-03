import { FC } from 'react';
import { FusionDatePicker, FusionDatePickerProps } from '@equinor/fusion-react-datepicker';
import { createStyles, makeStyles } from '@equinor/fusion-react-styles';

type ResourcesDatePickerProps = FusionDatePickerProps & {
    error?: boolean;
    errorMessage?: string;
};
const useStyles = makeStyles(() =>
    createStyles({
        message: {
            color: '#ff3b3b',
            fontSize: '11px',
            padding: '0 1rem',
            lineHeight: '1rem',
        },
        container: {
            '& > div:first-child': {
                height: '2.5rem',
            },
        },
    })
);
const ResourcesDatePicker: FC<ResourcesDatePickerProps> = ({
    error,
    errorMessage,
    date,
    onChange,
    ...props
}) => {
    const styles = useStyles();
    return (
        <div className={styles.container}>
            <FusionDatePicker
                date={date}
                onChange={onChange}
                isClearable={false}
                width={'100%'}
                showTodayButton
                placeholder="dd/mm/yyyy"
                showWeekNumbers
                {...props}
            />
            {error && <div className={styles.message}>{errorMessage}</div>}
        </div>
    );
};

export default ResourcesDatePicker;
