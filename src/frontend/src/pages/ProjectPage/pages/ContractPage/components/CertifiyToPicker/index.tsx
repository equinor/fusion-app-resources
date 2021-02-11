
import { RadioButton } from '@equinor/fusion-components';
import { FC, useState, useMemo, useCallback, useEffect } from 'react';
import * as styles from './styles.less';

type CertifyToPickerProps = {
    onChange: (selectedDate: Date) => void;
    defaultSelected?: CertifyToDateKey;
    isReCertification?: boolean;
};

type CertifyToDateKey = '1-month' | '6-months' | '12-months';

type CertifyToDate = {
    key: CertifyToDateKey;
    date: Date;
    title: string;
};

const getMonthFromNow = (monthsFormNow: number) =>
    new Date(new Date().setMonth(new Date().getMonth() + monthsFormNow));

const dates: CertifyToDate[] = [
    {
        key: '1-month',
        date: getMonthFromNow(1),
        title: '1 month',
    },
    {
        key: '6-months',
        date: getMonthFromNow(6),
        title: '6 months',
    },
    {
        key: '12-months',
        date: getMonthFromNow(12),
        title: '1 year',
    },
];

const CertifyToPicker: FC<CertifyToPickerProps> = ({ onChange, defaultSelected, isReCertification }) => {
    const [selectedDate, setSelectedDate] = useState<CertifyToDateKey>();
    const certifyPrefix = useMemo(() => (isReCertification ? 'Re-certify ' : 'Certify '), [
        isReCertification,
    ]);

    const onCertifyToChange = useCallback((dateItem?: CertifyToDate) => {
        if(!dateItem) {
            return
        }
        setSelectedDate(dateItem.key);
        onChange(dateItem.date);
    }, []);

    useEffect(() => {
        if(defaultSelected) {
            const dateItem = dates.find(d => d.key === defaultSelected);
            onCertifyToChange(dateItem);
        }
    }, [])

    return (
        <div className={styles.container}>
            {dates.map((date) => (
                <div className={styles.certifiedToItem} onClick={() => onCertifyToChange(date)}>
                    <RadioButton selected={date.key === selectedDate} />
                    <span>{`${certifyPrefix} for ${date.title}`}</span>
                </div>
            ))}
            <span className={styles.delegationFooter}>Choose time period from now</span>
        </div>
    );
};
export default CertifyToPicker;
