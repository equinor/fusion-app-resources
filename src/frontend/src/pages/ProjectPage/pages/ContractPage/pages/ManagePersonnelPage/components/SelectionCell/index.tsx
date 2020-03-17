import * as React from 'react';
import { CheckBox } from '@equinor/fusion-components';
import * as styles from './styles.less';

export type SelectionCellProps = {
  isSelected: boolean;
  onChange: () => void;
  isHovering?: boolean;
  indeterminate?: boolean;
};

const SelectionCell = React.forwardRef<
  HTMLDivElement | null,
  React.PropsWithChildren<SelectionCellProps>
>(
  (
    {
      isSelected,
      onChange,
      indeterminate,
    },
    ref
  ) => {
    return (
      <div
        className={styles.cell}
        ref={ref as React.MutableRefObject<HTMLDivElement | null>}
      >
        <CheckBox
          selected={isSelected}
          onChange={onChange}
          indeterminate={indeterminate}
        />
      </div>
    );
  }
);

export default SelectionCell;
