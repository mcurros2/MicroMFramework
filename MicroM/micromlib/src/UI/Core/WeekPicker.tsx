import { Group } from '@mantine/core';
import { Calendar, CalendarProps } from '@mantine/dates';
import dayjs from 'dayjs';
import { useState } from 'react';

function getDay(date: Date) {
    const day = date.getDay();
    return day === 0 ? 6 : day - 1;
}

function startOfWeek(date: Date) {
    return dayjs(new Date(date.getFullYear(), date.getMonth(), date.getDate() - getDay(date)))
        .startOf('day')
        .toDate();
}

function endOfWeek(date: Date) {
    return dayjs(new Date(date.getFullYear(), date.getMonth(), date.getDate() + (6 - getDay(date))))
        .endOf('day')
        .toDate();
}

function isInWeekRange(date: Date, value: Date | null) {
    if (!value) return false;
    const start = startOfWeek(value);
    const end = endOfWeek(value);
    return dayjs(date).isSame(start, 'day') || dayjs(date).isSame(end, 'day') ||
        (dayjs(date).isAfter(start) && dayjs(date).isBefore(end));
}

export interface WeekPickerProps extends CalendarProps {
    weekStartDatevalue: Date | null,
    setWeekStartValue: (value: Date | null) => void,
    setWeekEndValue?: (value: Date | null) => void,
    allowDeselect?: boolean,
}

export function WeekPicker(props: WeekPickerProps) {
    const {
        weekStartDatevalue, setWeekStartValue,
        setWeekEndValue, allowDeselect,
        ...others } = props;

    const [hovered, setHovered] = useState<Date | null>(null);

    return (
        <Group position="center">
            <Calendar
                placeholder={undefined} onPointerEnterCapture={undefined} onPointerLeaveCapture={undefined}
                {...others}
                withCellSpacing={false}
                getDayProps={(date) => {
                    const isHovered = isInWeekRange(date, hovered);
                    const isSelected = isInWeekRange(date, weekStartDatevalue);
                    const isInRange = isHovered || isSelected;

                    return {
                        onMouseEnter: () => setHovered(date),
                        onMouseLeave: () => setHovered(null),
                        inRange: isInRange,
                        firstInRange: isInRange && date.getDay() === 1,
                        lastInRange: isInRange && date.getDay() === 0,
                        selected: isSelected,
                        onClick: () => {
                            if (allowDeselect && isSelected) {
                                setWeekStartValue(null);
                                if (setWeekEndValue) setWeekEndValue(null);
                            } else {
                                setWeekStartValue(startOfWeek(date));
                                if (setWeekEndValue) setWeekEndValue(endOfWeek(date));
                            }
                        },
                    };
                }} />
        </Group>
    );
}